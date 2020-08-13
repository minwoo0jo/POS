using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows.Forms.VisualStyles;
using System.Windows.Forms;
using System.IO;

namespace Point_of_Sale.Resources
{
    class SerialPortController
    {
        private SerialPort spDisplay;
        private SerialPort spPrinter;
        
        public SerialPortController()
        {
            spDisplay = new SerialPort("COM7", 9600, Parity.None, 8, StopBits.One);
            spPrinter = new SerialPort("COM8", 9600, Parity.None, 8, StopBits.One);
        }

        public void displayText(String text)
        {
            try
            {
                spDisplay.Open();
                spDisplay.Write((char)12 + "");
                if (text.Length <= 20)
                    spDisplay.Write(text);
                else
                {
                    String[] textSplit = text.Split(' ');
                    String full = "";
                    foreach (String s in textSplit)
                    {
                        if (full.Length <= 20 && full.Length + s.Length > 20)
                        {
                            while (full.Length < 20)
                                full += " ";
                        }
                        full += s + " ";
                    }
                    spDisplay.Write(full);
                }
                spDisplay.Close();
            }
            catch (IOException)
            {
                return;
            }
        }

        //Print the receipt for the order
        public void printOrder(frmMain parent)
        {
            try
            {
                DateTime now = DateTime.Now;
                Double subtotal, tax;
                subtotal = 0;
                tax = 0;
                String subtotalStr, taxStr, total, paid, change, buffer, line, justification, format, cutReceipt;
                total = parent.btnSumAmt.Text;
                paid = parent.txReceived.Text;
                change = parent.txChange.Text;
                justification = (char)27 + "" + "a";
                format = (char)27 + "" + (char)33;
                cutReceipt = (char)29 + "" + (char)86 + "1";
                buffer = "                                                        ";
                line = "--------------------------------------------------------";
                spPrinter.Open();
                /*
                 * Printer text justification basic formula: (27 + a + n) where 0 <= n <= 2
                 * n is as follows:
                 * 0 = left
                 * 1 = center
                 * 2 = right
                 */
                spPrinter.Write(justification + "1");
                /*
                 * Printer formatting basic formula: (27 + 33 + n) where 0 <= n <= 255
                 * n is the sum as follows:
                 * character font (12x24)   0
                 * character font (9x17)    1
                 * bold font                8
                 * double height            16
                 * double width             32
                 * underline mode           128
                 */
                spPrinter.Write(format + (char)48);
                spPrinter.WriteLine("Four Seasons Donuts\n");
                spPrinter.Write(format + (char)9);
                spPrinter.WriteLine("3511 N. Belt Line Rd\nIrving, TX 75062\n(972) 255-0468\n");
                spPrinter.WriteLine(now.ToShortDateString() + " " + now.ToShortTimeString() + "\nSales Receipt ID: " + GlobalVar.CURRENT_ORDER.orderId);
                spPrinter.Write("Item" + buffer.Substring(0, 23));
                spPrinter.Write("Qty" + buffer.Substring(0, 4));
                spPrinter.Write("Price" + buffer.Substring(0, 4));
                spPrinter.Write("D/C" + buffer.Substring(0, 4));
                spPrinter.WriteLine("Amount" + buffer.Substring(0, 0));
                spPrinter.WriteLine(line);
                foreach (DataGridViewRow row in parent.orderList.Rows)
                {
                    tax += Double.Parse(row.Cells["tax_amt"].Value.ToString());
                    String name = row.Cells["item_name"].Value.ToString();
                    String qty = row.Cells["qty"].Value.ToString();
                    String price = row.Cells["price"].Value.ToString();
                    String dc = row.Cells["dc_amt"].Value.ToString();
                    String amt = row.Cells["amt"].Value.ToString();
                    if (name.Length > 19)
                        name = name.Substring(0, 19);
                    spPrinter.Write(name + buffer.Substring(0, 23 - name.Length));
                    spPrinter.Write(buffer.Substring(0, 7 - qty.Length) + qty);
                    spPrinter.Write(buffer.Substring(0, 9 - price.Length) + price);
                    spPrinter.Write(buffer.Substring(0, 7 - dc.Length) + dc);
                    spPrinter.WriteLine(buffer.Substring(0, 10 - amt.Length) + amt);
                }
                spPrinter.WriteLine(line);
                subtotal = Double.Parse(total) - tax;
                subtotalStr = String.Format("{0:0.00}", subtotal);
                taxStr = String.Format("{0:0.00}", tax);
                spPrinter.Write("Subtotal:" + buffer.Substring(0, 56 - subtotalStr.Length - 9) + subtotalStr);
                spPrinter.Write("Tax:" + buffer.Substring(0, 56 - taxStr.Length - 4) + taxStr);
                spPrinter.Write("Total:" + buffer.Substring(0, 56 - total.Length - 6) + total);
                spPrinter.Write("Amount Paid:" + buffer.Substring(0, 56 - paid.Length - 12) + paid);
                spPrinter.WriteLine("Change Due:" + buffer.Substring(0, 56 - change.Length - 11) + change + "\n\n");
                spPrinter.Write(justification + "1");
                spPrinter.Write(format + (char)24);
                spPrinter.WriteLine("Thank you for shopping with us");
                spPrinter.WriteLine("\n\n\n\n");
                spPrinter.Write(cutReceipt);
                spPrinter.Close();
            }
            catch (IOException)
            {
                MessageBox.Show("Please connect a printer before attempting to print a recept.", "Error: No Printer Found");
            }
        }

    }
}
