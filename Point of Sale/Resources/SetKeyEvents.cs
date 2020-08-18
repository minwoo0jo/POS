using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;

namespace Point_of_Sale.Resources
{
    //Class that handles various button clicks for the POS
    class SetKeyEvents
    {
        //Event when an item button is clicked.
        public void btn_ItemClick(object sender, EventArgs e, frmMain parent)
        {
            frmMain frm = parent;
            //If an item from the order list is currently in focus, it should be deselected before focusing on the clicked item.
            frm.orderList.ClearSelection();
            ArrayList dtlList = GlobalVar.DTL_LIST;
            frm.runSound();
            Button b = (Button)sender;
            Hashtable dtlinfo;
            //Use the button's tag to find the correct item from the global list of all items
            for (int i = 0; i < dtlList.Count; i++)
            {
                dtlinfo = (Hashtable)dtlList[i];
                //Keep the item's info itself in a global variable so the loop is only necessary once
                GlobalVar.FOCUS_ITEM = dtlinfo;
                if (b.Tag.ToString().Equals(dtlinfo["item_cd"]))
                {
                    frm.txItemName.Text = dtlinfo["short_name"].ToString();
                    frm.txPrice.Text = dtlinfo["price"].ToString();
                    frm.txQty.Text = "0";
                    frm.txTax.Text = dtlinfo["tax_flag"].ToString().Equals("Y") ? GlobalVar.TAX_RATE.ToString() : "0.00";
                    frm.txTaxTtl.Text = "0.00";
                    frm.txDc.Text = "0.00";
                    frm.txDcTtl.Text = "0.00";
                    frm.txItemTtl.Text = "0.00";
                    break;
                }
            }
        }

        //Event when a row in the order list is clicked.
        public void btn_OrderListClick(object sender, EventArgs e, frmMain parent)
        {
            frmMain frm = parent;
            frm.runSound();
            DataGridView orderList = (DataGridView)sender;
            //Put the selected row back in focus, but keep the row selected so the system updates the item instead of adding it again.
            DataGridViewRow row = orderList.SelectedRows[0];
            frm.txItemName.Text = row.Cells["item_name"].Value.ToString();
            frm.txQty.Text = row.Cells["qty"].Value.ToString();
            frm.txPrice.Text = row.Cells["price"].Value.ToString();
            frm.txTax.Text = row.Cells["tax_rate"].Value.ToString();
            frm.txTaxTtl.Text = row.Cells["tax_amt"].Value.ToString();
            frm.txDc.Text = row.Cells["dc_ratio"].Value.ToString();
            frm.txDcTtl.Text = row.Cells["dc_amt"].Value.ToString();
            frm.txItemTtl.Text = row.Cells["amt"].Value.ToString();
            if (Double.Parse(row.Cells["dc_amt"].Value.ToString()) > 0)
                GlobalVar.DISCOUNT = true;
            ArrayList dtlList = GlobalVar.DTL_LIST;
            Hashtable dtlinfo;
            for (int i = 0; i < dtlList.Count; i++)
            {
                dtlinfo = (Hashtable)dtlList[i];
                if (row.Cells["item_cd"].Value.ToString().Equals(dtlinfo["item_cd"]))
                {
                    GlobalVar.FOCUS_ITEM = dtlinfo;
                    break;
                }
            }
        }

        //Event when a number button is clicked.
        //Can be used to change item quantity value or amount received value.
        public void btn_NumberClick(object sender, EventArgs e, frmMain parent)
        {
            frmMain frm = parent;
            frm.runSound();
            int qty;
            //If there is no focus item, then the input is for the amount received.
            if (GlobalVar.FOCUS_ITEM == null)
            {
                //No need to initiate payment if there is nothing in the order.
                if (frm.orderList.Rows.Count == 0)
                    return;
                if (frm.txReceived.Text != null && !frm.txReceived.Text.Equals(""))
                {
                    //Since qty is an int, any dollar amount must be multiplied by 100 to prevent loss of data.
                    if (frm.txReceived.Text.Length > 0)
                        qty = (int)(Double.Parse(frm.txReceived.Text) * 100);
                    else
                        qty = 0;
                }
                else
                    qty = 0;

            }
            //If there is a focus item, then the input is for changing the quantity of the item.
            else
            {
                if (frm.txQty.Text != null && !frm.txQty.Text.Equals(""))
                    qty = Int32.Parse(frm.txQty.Text);
                else
                    qty = 0;
            }


            Button b = (Button)sender;
            //Use if/else statements to perform the appropriate functions on the quantity
            if (b.Name.StartsWith("Number"))
            {
                int add = Int32.Parse(b.Name.Substring(6));
                qty = qty * 10 + add;
                if (b.Name.Equals("Number00"))
                    qty = qty * 10 + add;
            }
            else if (b.Name.StartsWith("Dozen"))
            {
                qty = 12 * Int32.Parse(b.Name.Substring(5));
            }
            else if (b.Name.StartsWith("Backspace"))
            {
                if(qty > 0)
                    qty /= 10;
            }
            else if (b.Name.StartsWith("Clear"))
            {
                qty = 0;
            }

            //If there is no focus item, divide qty by 100 to get the correct dollar figure.
            if (GlobalVar.FOCUS_ITEM == null)
                frm.txReceived.Text = String.Format("{0:0.00}", ((double)qty / 100));
            //If there is a focus item, calculate the correct tax and discount while updating the qty.
            else
            {
                frm.txQty.Text = qty.ToString();
                //subtotal + tax - discount = total
                double subtotal = (qty * Double.Parse(frm.txPrice.Text));
                double discountR;
                //Check if discount should be applied, then calculate discount ratio using the dz_amt value
                if (frm.discount())
                {
                    double dz_amtDc = Double.Parse(GlobalVar.FOCUS_ITEM["dz_amt"].ToString());
                    double dz_amt = Double.Parse(frm.txPrice.Text) * 12;
                    Console.WriteLine(dz_amtDc + " " + dz_amt);
                    discountR = 100 * ((dz_amt - dz_amtDc) / dz_amt);
                }
                else
                    discountR = 0;
                double discount = subtotal * discountR / 100;
                double tax = subtotal * (Double.Parse(frm.txTax.Text) / 100);
                frm.txDc.Text = String.Format("{0:#0.00}", discountR);
                frm.txTaxTtl.Text = String.Format("{0:0.00}", tax);
                frm.txDcTtl.Text = String.Format("{0:0.00}", discount);
                frm.txItemTtl.Text = String.Format("{0:0.00}", subtotal + tax - discount);
            }
        }

        //Event when the enter button is clicked.
        //Can be used to enter in a new item or to update an existing item.
        public void btn_EnterClick(object sender, EventArgs e, frmMain parent)
        {
            frmMain frm = parent;
            frm.runSound();
            if(!GlobalVar.CURRENT_ORDER.editable)
            {
                MessageBox.Show("This order cannot be edited. Please start a new order.", "Error: Order Already Complete");
                return;
            }
            //If there is a focus item which has a qty of 1 or higher, then add this item to the order list.
            if(GlobalVar.FOCUS_ITEM != null && !frm.txQty.Text.Equals("0"))
            {
                String name, code, qty, price, taxR, tax, dcR, dc, amt;
                name = frm.txItemName.Text;
                code = GlobalVar.FOCUS_ITEM["item_cd"].ToString();
                amt = frm.txItemTtl.Text;
                qty = frm.txQty.Text;
                price = frm.txPrice.Text;
                taxR = frm.txTax.Text;
                tax = frm.txTaxTtl.Text;
                dcR = frm.txDc.Text;
                dc = frm.txDcTtl.Text;
                //If there is a selected row, then this is not an addition to the order, but an update to the order.
                if(frm.orderList.SelectedRows.Count == 1)
                {
                    frm.orderList.SelectedRows[0].SetValues(name, code, qty, price, taxR, tax, dcR, dc, amt);
                    frm.orderList.ClearSelection();
                    frm.dispSum();
                }
                //If there is no selected row, then this is an addition to the order.
                //If this item is already in the order, then prompt the user for confirmation in case of misclicks.
                else if(!frm.CheckDuplicateItems(name))
                {
                    frm.orderList.Rows.Add(name, code, qty, price, taxR, tax, dcR, dc, amt);
                    frm.orderList.ClearSelection();
                    frm.dispSum();
                }
                GlobalVar.FOCUS_ITEM = null;
                frm.clearScreen(false);
            }
            //If there is no focus item, then initiate payment instead of adding to the order.
            else if(GlobalVar.FOCUS_ITEM == null && !(frm.txReceived.Text.Equals("") || frm.txReceived.Text.Equals("0")))
            {
                double total = Double.Parse(frm.btnSumAmt.Text);
                double received = Double.Parse(frm.txReceived.Text);
                String change = String.Format("{0:0.00}", received - total);
                frm.txChange.Text = change;
                GlobalVar.CURRENT_ORDER.amountPaid = received;
                GlobalVar.CURRENT_ORDER.change = received - total;
                if (GlobalVar.CURRENT_ORDER.paymentType.Equals("N"))
                {
                    if (received >= total)
                        GlobalVar.CURRENT_ORDER.paymentComplete = true;
                    else
                        GlobalVar.CURRENT_ORDER.paymentComplete = false;
                }
            }
        }


        //Event when the delete row button is clicked.
        public void btn_DeleteRow(object sender, EventArgs e, frmMain parent)
        {
            frmMain frm = parent;
            frm.runSound();
            //Make sure a row is selected before deleting.
            if (frm.orderList.SelectedRows.Count == 0)
            {
                MessageBox.Show("You must select a row to delete a row", "Error: No selected rows");
                return;
            }
            DataGridViewRow row = frm.orderList.SelectedRows[0];
            frm.orderList.Rows.Remove(row);
            frm.dispSum();
            frm.clearScreen(false);
        }

        //Event when the delete all button is clicked.
        public void btn_DeleteAll(object sender, EventArgs e, frmMain parent)
        {
            frmMain frm = parent;
            frm.runSound();
            //Do not delete all without prompting the user again for confirmation.
            var result = MessageBox.Show("Are you sure you want to delete all?", "Warning: Delete All", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
                return;
            frm.orderList.Rows.Clear();
            frm.dispSum();
            frm.clearScreen(false);
        }
    }

}
