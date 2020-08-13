using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Resources;
using System.Collections;
using System.Media;
using Point_of_Sale.Resources;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Collections.Generic;

namespace Point_of_Sale
{
    public partial class frmMain : Form
    {

        public frmMain()
        {
            InitializeComponent();
            GlobalVar.CURRENT_ORDER = new Order();
            GlobalVar.ACTIVE_ORDERS = new List<Order>
            {
                GlobalVar.CURRENT_ORDER
            };
            GlobalVar.SP_CONTROLLER = new SerialPortController();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        private void frmMain_Load(object sender, EventArgs e)
        {
            GetMasterInfo mst = new GetMasterInfo();
            txItemName.Font = new Font("Algerian", 26, FontStyle.Bold);
            this.WindowState = FormWindowState.Maximized;
            mst.loadItemList();
            getOrderGridView();
            //orderList.DataSource = getOrderGridView();
            initMstBtn();
        }

        //At startup, load all the item category buttons.
        private void initMstBtn()
        {
            ArrayList mstList = GlobalVar.MST_LIST;
            String itemid;
            String btnid;
            String lblid;
            string pnlid;
            Button btn;
            Panel pnl;
            //Label lbl;
            Hashtable mstinfo;
            Boolean isCallSubMenu = false;
            for (int i = 0; i < 21; i++)
            {
                btnid = "btnM" + (i + 1).ToString("00");
                //pnlid = "pnl" + (i + 1).ToString("00");
                //lblid = "lbm" + (i + 1).ToString("00");
                btn = this.Controls.Find(btnid, true).FirstOrDefault() as Button;
                //lbl = this.Controls.Find(lblid, true).FirstOrDefault() as Label;
                //pnl = this.Controls.Find(pnlid, true).FirstOrDefault() as Panel;

                if (i >= mstList.Count)
                {
                    btn.Visible = false;
                    continue;
                }

                mstinfo = (Hashtable)mstList[i];
                itemid = (String)mstinfo["item_cd"];
                if (!isCallSubMenu)
                {
                    isCallSubMenu = true;
                    initSubBtn(itemid);
                }
                Bitmap img = new Bitmap("./image/"+itemid+".jpg");

                btn.Tag = itemid;
                //btn.Text = (String)mstinfo["short_name"];
                btn.Font = new Font(btn.Font.Name, btn.Font.Size, FontStyle.Bold);
                btn.TextAlign = ContentAlignment.BottomCenter;

                btn.Image = ResizeImage(img, btn.Width, btn.Height);
                btn.BackgroundImageLayout = ImageLayout.Stretch;
                btn.Visible = true;
                try
                {
                    //pnl.Parent = btn;
                    //pnl.BackColor = Color.FromArgb(80, 0, 0, 0);
                    //lbl.Parent = btn;
                    //lbl.BackColor = Color.FromArgb(80, 0, 0, 0);
                    Console.WriteLine((String)mstinfo["short_name"]);
                    btn.Text = (String)mstinfo["short_name"];
                    Console.WriteLine("Button Location : X[" + btn.Location.X + "], Y[" + btn.Location.Y + "]");
                    //lbl.Location = new Point ( btn.Location.X, btn.Size.Height - 20);
                    //Console.WriteLine("Panel Location : X[" + pnl.Location.X + "], Y[" + pnl.Location.Y + "]");
                }
                catch
                {

                }

            }
        }

        //When an item category is clicked, load the items in that category.
        private void initSubBtn(String p_mstcode)
        {
            ArrayList dtlList = GlobalVar.DTL_LIST;
            String itemid;
            String btnid;
            Button btn; Hashtable dtlinfo;
            int btnno = 0;
            String mstid;
            for (int i = 0; i < 24; i++)
            {
                btnid = "btnS" + (i + 1).ToString("00");
                btn = this.Controls.Find(btnid, true).FirstOrDefault() as Button;
                btn.Visible = false;
            }

            for (int i = 0; i < dtlList.Count; i++)
            {
                dtlinfo = (Hashtable)dtlList[i];
                itemid = (String)dtlinfo["item_cd"];
                mstid = (String)dtlinfo["parent_cd"];
                if (!p_mstcode.Equals(mstid)) continue;
                btnno++;
                btnid = "btnS" + (btnno).ToString("00");
                btn = this.Controls.Find(btnid, true).FirstOrDefault() as Button;
                Bitmap img = new Bitmap("./image/" + itemid + ".jpg");
                btn.Tag = itemid;
                btn.Text = (String)dtlinfo["short_name"];
                btn.Font = new Font(btn.Font.Name, btn.Font.Size, FontStyle.Bold);
                btn.TextAlign = ContentAlignment.BottomCenter;
                btn.Image = ResizeImage(img, btn.Width, btn.Height);

                btn.Visible = true;
            }
        }

        private void getOrderGridView()
        {
            dispSum();
        }

        //Update the total values on the screen whenever an item is added or updated.
        public void dispSum()
        {
            decimal[] sum = new decimal[3];

            for (int i = 0; i < orderList.Rows.Count; i++)
            {
                sum[0] += Convert.ToDecimal(orderList.Rows[i].Cells["qty"].Value);
                sum[1] += Convert.ToDecimal(orderList.Rows[i].Cells["amt"].Value);
                sum[2] += Convert.ToDecimal(orderList.Rows[i].Cells["dc_amt"].Value);
            }
            btnSumCnt.Text = (orderList.Rows.Count).ToString() + " / " + (sum[0]).ToString();
            btnSumAmt.Text = String.Format("{0:0.00}", sum[1]);
            GlobalVar.CURRENT_ORDER.total = (double)sum[1];

            //If an item was added after payment was already received, automatically update the change amount.
            //Change amount can be negative if the payment from customer is insufficient.
            if (GlobalVar.CURRENT_ORDER.paymentType.Equals("N"))
            {
                if (txChange.Text != null && txChange.Text.Length > 0)
                {
                    double received = Double.Parse(txReceived.Text);
                    txChange.Text = String.Format("{0:0.00}", received - (double)sum[1]);
                    if (received > (double)sum[1])
                        GlobalVar.CURRENT_ORDER.paymentComplete = true;
                    else
                        GlobalVar.CURRENT_ORDER.paymentComplete = false;
                }
            }
            else
            {
                GlobalVar.CURRENT_ORDER.amountPaid = (double)sum[1];
            }
            GlobalVar.CURRENT_ORDER.total = (double)sum[1];

            //If there are no items in the order yet, display a welcome message instead of the total.
            if((double)sum[1] < 0.01)
                GlobalVar.SP_CONTROLLER.displayText("Welcome to Four Seasons Donuts!");
            //If there are items in the order, display the total cost on the left side and the amount discounted in the right side.
            else
            {
                String display = "Total      Discount " + sum[1].ToString();
                //Pad the middle of the second row with spaces so the total is on the left and the discount is on the right.
                for(int i = sum[1].ToString().Length; i < 19 - sum[2].ToString().Length; i++)
                {
                    display += " ";
                }
                GlobalVar.SP_CONTROLLER.displayText(display + sum[2].ToString()); ;
            }
            GlobalVar.CURRENT_ORDER.orderList = new Dictionary<int, Dictionary<string, string>>();
            GlobalVar.CURRENT_ORDER.orderList = copyToOrderList(orderList, GlobalVar.CURRENT_ORDER.orderList);
        }

        //Whenever any item is added, this function checks if it already exists in the current order.
        //If it already exists, prompt the user again to make sure it's not a misclick.
        public Boolean CheckDuplicateItems(String item)
        {
            foreach(DataGridViewRow row in orderList.Rows) {
                if(row.Cells["item_name"].Value.ToString().Equals(item))
                {
                    var result = MessageBox.Show("This item is already in the order. Do you want to add this item again?", "Warning: Duplicate Item", MessageBoxButtons.YesNo);
                    return result == DialogResult.No;
                }
            }
            return false;
        }

        //Handler for when an item category button is clicked.
        private void btn_Click(object sender, EventArgs e)
        {
            runSound();
            Button b = (Button)sender;
            initSubBtn(b.Tag.ToString());
        }

        //Handler for when an item button is clicked.
        private void btn_ItemClick(object sender, EventArgs e)
        {
            SetKeyEvents sk = GlobalVar.EVENTS;
            //Since there could be a selected row in the orderlist, the handler for the orderlist is disabled for this method.
            orderList.SelectionChanged -= this.btn_OrderListClick;
            sk.btn_ItemClick(sender, e, this);
            orderList.SelectionChanged += this.btn_OrderListClick;
            //Discount is defaulted to false and only changes when forced with the discount button or if the qty is 12 or higher.
            GlobalVar.DISCOUNT = false;
        }

        //Handler for when a number button is clicked.
        private void btn_NumberClick(object sender, EventArgs e)
        {
            SetKeyEvents sk = GlobalVar.EVENTS;
            sk.btn_NumberClick(sender, e, this);
        }

        //Handler for when the discount button is clicked.
        private void btn_DiscountClick(object sender, EventArgs e)
        {
            if (GlobalVar.FOCUS_ITEM == null)
                return;
            GlobalVar.DISCOUNT = !GlobalVar.DISCOUNT;
            btn_NumberClick(sender, e);
        }

        //Handler for when the enter button is clicked.
        private void btn_EnterClick(object sender, EventArgs e)
        {
            SetKeyEvents sk = GlobalVar.EVENTS;
            //Since there could be a selected row in the orderlist, the handler for the orderlist is disabled for this method.
            orderList.SelectionChanged -= this.btn_OrderListClick;
            sk.btn_EnterClick(sender, e, this);
            orderList.SelectionChanged += this.btn_OrderListClick;
            GlobalVar.DISCOUNT = false;
        }

        //Handler for when the back order button is clicked.
        private void btn_BackOrder(object sender, EventArgs e)
        {
            runSound();
            //Since there could be a selected row in the orderlist, the handler for the orderlist is disabled for this method.
            orderList.SelectionChanged -= this.btn_OrderListClick;
            GetMasterInfo mst = new GetMasterInfo();
            GlobalVar.CURRENT_ORDER = mst.RetrieveOrder(GlobalVar.CURRENT_ORDER.now, this.orderList, true);
            orderList.Rows.Clear();
            orderList = copyToOrderList(GlobalVar.CURRENT_ORDER.orderList, orderList);
            orderList.SelectionChanged += this.btn_OrderListClick;
        }

        //Handler for when the forward order button is clicked.
        private void btn_ForwardOrder(object sender, EventArgs e)
        {
            runSound();
            if (orderList.Rows.Count == 0 || GlobalVar.CURRENT_ORDER.editable)
                return;
            //Since there could be a selected row in the orderlist, the handler for the orderlist is disabled for this method.
            orderList.SelectionChanged -= this.btn_OrderListClick;
            GetMasterInfo mst = new GetMasterInfo();
            GlobalVar.CURRENT_ORDER = mst.RetrieveOrder(GlobalVar.CURRENT_ORDER.now, this.orderList, false);
            orderList.Rows.Clear();
            orderList = copyToOrderList(GlobalVar.CURRENT_ORDER.orderList, orderList);
            orderList.SelectionChanged += this.btn_OrderListClick;
        }

        //Copy the order from dictionary to datagridview
        public Dictionary<int, Dictionary<String, String>> copyToOrderList(DataGridView from, Dictionary<int, Dictionary<String, String>> to)
        {
            foreach(DataGridViewRow row in from.Rows)
            {
                Dictionary<String, String> orderItem = new Dictionary<String, String>
                {
                    { "item_name", row.Cells["item_name"].Value.ToString() },
                    { "item_cd", row.Cells["item_cd"].Value.ToString() },
                    { "qty", row.Cells["qty"].Value.ToString() },
                    { "price", row.Cells["price"].Value.ToString() },
                    { "tax_rate", row.Cells["tax_rate"].Value.ToString() },
                    { "tax_amt", row.Cells["tax_amt"].Value.ToString() },
                    { "dc_ratio", row.Cells["dc_ratio"].Value.ToString() },
                    { "dc_amt", row.Cells["dc_amt"].Value.ToString() },
                    { "amt", row.Cells["amt"].Value.ToString() },
                };
                to.Add(to.Count, orderItem);
            }
            return to;
        }

        //Copy the order from datagridview to dictionary
        public DataGridView copyToOrderList(Dictionary<int, Dictionary<String, String>> from, DataGridView to)
        {
            for(int i = 0; i < from.Count; i++)
            {
                Dictionary<String, String> orderItem = from[i];
                to.Rows.Add(orderItem["item_name"],
                    orderItem["item_cd"],
                    orderItem["qty"],
                    orderItem["price"],
                    orderItem["tax_rate"],
                    orderItem["tax_amt"],
                    orderItem["dc_ratio"],
                    orderItem["dc_amt"],
                    orderItem["amt"]);
            }
            return to;
        }

        //Handler for when the save button is clicked.
        private void btn_Save(object sender, EventArgs e)
        {
            if (!GlobalVar.CURRENT_ORDER.editable)
            {
                MessageBox.Show("This order has already been saved. Please start a new order.", "Error: Order Already Complete");
                return;
            }
            //If there is no order to save, do not save the order.
            //If payment has not completed, do not save the order.
            //If payment is insufficient, do not save the order.
            if (!GlobalVar.CURRENT_ORDER.paymentComplete)
            {
                MessageBox.Show("Please complete payment before attempting to finalize the order.", "Error: Payment Not Received");
                return;
            }
            GetMasterInfo mst = new GetMasterInfo();
            //Send it to a function that saves the order to the database.
            mst.SaveNewOrder(GlobalVar.CURRENT_ORDER);

            GlobalVar.FOCUS_ITEM = null;
            GlobalVar.DISCOUNT = false;

            GlobalVar.CURRENT_ORDER = new Order();
            Image newPaymentType = new Bitmap(@"..\..\Resources\Cash.jpg");
            btnPayment.BackgroundImage = newPaymentType;
            orderList.Rows.Clear();
            dispSum();
            txPrice.Text = "";
            txQty.Text = "";
            txTax.Text = "";
            txTaxTtl.Text = "";
            txDc.Text = "";
            txDcTtl.Text = "";
            txItemName.Text = "";
            txItemTtl.Text = "";
            txReceived.Text = "";
            txChange.Text = "";

        }

        //Handler for when the print button is clicked.
        private void btn_Print(object sender, EventArgs e)
        {
            if(!GlobalVar.CURRENT_ORDER.paymentComplete)
            {
                MessageBox.Show("Please complete payment before attempting to print the receipt.", "Error: Payment Not Received");
                return;
            }
            GlobalVar.SP_CONTROLLER.printOrder(this);
        }

        //Handler for when a row in the order list is clicked.
        private void btn_OrderListClick(object sender, EventArgs e)
        {
            SetKeyEvents sk = GlobalVar.EVENTS;
            sk.btn_OrderListClick(sender, e, this);
        }

        //Handler for when the delete row button is clicked.
        private void btn_DeleteRow(object sender, EventArgs e)
        {
            //Since there could be a selected row in the orderlist, the handler for the orderlist is disabled for this method.
            orderList.SelectionChanged -= btn_OrderListClick;
            SetKeyEvents sk = GlobalVar.EVENTS;
            sk.btn_DeleteRow(sender, e, this);
            orderList.SelectionChanged += btn_OrderListClick;
        }

        //Handler for when the delete all button is clicked.
        private void btn_DeleteAll(object sender, EventArgs e)
        {
            //Since there could be a selected row in the orderlist, the handler for the orderlist is disabled for this method.
            orderList.SelectionChanged -= btn_OrderListClick;
            SetKeyEvents sk = GlobalVar.EVENTS;
            sk.btn_DeleteAll(sender, e, this);
            GlobalVar.ACTIVE_ORDERS.Remove(GlobalVar.CURRENT_ORDER);
            GlobalVar.CURRENT_ORDER = new Order();
            orderList.SelectionChanged += btn_OrderListClick;
        }

        //Handler for changing the payment method.
        private void btn_ChangePayment(object sender, EventArgs e)
        {
            String type = GlobalVar.CURRENT_ORDER.ChangePayment();
            Console.WriteLine(type);
            Image newPaymentType = new Bitmap(@"..\..\Resources\" + type + ".jpg");
            btnPayment.BackgroundImage = newPaymentType;
            if (!type.Equals("Cash"))
            {
                GlobalVar.CURRENT_ORDER.paymentComplete = true;
                GlobalVar.CURRENT_ORDER.amountPaid = GlobalVar.CURRENT_ORDER.total;
                txReceived.Text = btnSumAmt.Text;
                txChange.Text = "0.00";
            }
            else
            {
                GlobalVar.CURRENT_ORDER.paymentComplete = false;
                GlobalVar.CURRENT_ORDER.amountPaid = 0;
                txReceived.Text = "";
                txChange.Text = "";
            }
        }

        //Method to check if the discount should be applied for the focus item.
        public Boolean discount()
        {
            //Discount is applied if the qty is 12 or higher.
            //Discount can be forcibly applied by the user.
            return Int32.Parse(txQty.Text) >= 12 || GlobalVar.DISCOUNT;
        }

        public void clearScreen(bool full)
        {
            txItemName.Text = "";
            txQty.Text = "";
            txPrice.Text = "";
            txTax.Text = "";
            txTaxTtl.Text = "";
            txDc.Text = "";
            txDcTtl.Text = "";
            txItemTtl.Text = "";
            if(full)
            {
                orderList.Rows.Clear();
                btnSumAmt.Text = "0.00";
                btnSumCnt.Text = "0 / 0";
                txReceived.Text = "";
                txChange.Text = "";
            }
        }

        public void runSound()
        {
            SoundPlayer snd = new SoundPlayer(Properties.Resources.type);
            snd.PlaySync();
        }



        private void button16_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button28_Click(object sender, EventArgs e)
        {
            runSound();
        }

        private void button29_Click(object sender, EventArgs e)
        {
            runSound();
        }


        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width -5, height-5);
            var destImage = new Bitmap(width-5, height-5);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

    }
}
