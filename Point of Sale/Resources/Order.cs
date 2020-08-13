using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Point_of_Sale.Resources
{
    class Order
    {
        public Dictionary<int, Dictionary<String, String>> orderList;
        public bool paymentComplete;
        public String paymentType;
        public double total;
        public double amountPaid;
        public double change;
        public DateTime now;
        public String orderId;
        public bool editable;
        public Order()
        {
            orderList = new Dictionary<int, Dictionary<String, String>>();
            paymentComplete = false;
            paymentType = "N";
            total = 0;
            amountPaid = 0;
            change = 0;
            editable = true;
            now = DateTime.Now;
            //force yymmddhhmmss format even if the values are not 2 digits long
            orderId = now.Year % 100 + "" 
                + now.Month.ToString("D2") 
                + now.Day.ToString("D2") 
                + now.Hour.ToString("D2") 
                + now.Minute.ToString("D2") 
                + now.Second.ToString("D2") 
                + GlobalVar.SYSTEM_NAME.Substring(GlobalVar.SYSTEM_NAME.Length - 3);
        }

        public Order(Dictionary<int, Dictionary<String, String>> orderList, String paymentType, double total, double amountPaid, double change, String orderId, DateTime now)
        {
            this.orderList = orderList;
            this.paymentComplete = true;
            this.paymentType = paymentType;
            this.total = total;
            this.amountPaid = amountPaid;
            this.change = change;
            editable = false;
            this.orderId = orderId;
            this.now = now;
        }

        //Cycle through payment options in order of Cash, Card, and Delivery
        public String ChangePayment()
        {
            if (paymentType.Equals("N"))
            {
                paymentType = "Y";
                return "Card";
            }
            else if (paymentType.Equals("Y"))
            {
                paymentType = "D";
                return "Delivery";
            }
            else if (paymentType.Equals("D"))
            {
                paymentType = "N";
                return "Cash";
            }
            return paymentType;
        }
    }
}
