using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Exceptions;
using NBrightCore.common;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Components;

namespace Nevoweb.DNN.NBrightStore
{
    public class NBrightPxPayPaymentProvider : Nevoweb.DNN.NBrightBuy.Components.Interfaces.PaymentsInterface
    {
        public override string Paymentskey { get; set; }

        public override string GetTemplate(NBrightInfo cartInfo)
        {
            var info = ProviderUtils.GetProviderSettings("NBrightPxPaypayment");
            var templ = ProviderUtils.GetTemplateData(info.GetXmlProperty("genxml/textbox/checkouttemplate"), info);

            return templ;
        }

        public override string RedirectForPayment(OrderData orderData)
        {
            orderData.OrderStatus = "020";
            orderData.PurchaseInfo.SetXmlProperty("genxml/paymenterror", "");
            orderData.PurchaseInfo.Lang = Utils.GetCurrentCulture();
            orderData.SavePurchaseData();
            try
            {

                var settings = ProviderUtils.GetProviderSettings("NBrightPxPaypayment");

                string PxPayUserId = settings.GetXmlProperty("genxml/textbox/pxpayuserid");
                string PxPayKey = settings.GetXmlProperty("genxml/textbox/pxpaykey");

                PxPay WS = new PxPay(PxPayUserId, PxPayKey);

                RequestInput input = new RequestInput();

                var appliedtotal = orderData.PurchaseInfo.GetXmlPropertyDouble("genxml/appliedtotal");
                var alreadypaid = orderData.PurchaseInfo.GetXmlPropertyDouble("genxml/alreadypaid");

                var orderTotal = (appliedtotal - alreadypaid).ToString("0.00");

                var param = new string[3];
                param[0] = "orderid=" + orderData.PurchaseInfo.ItemID.ToString("");
                param[1] = "status=1";
                var returnUrl = Globals.NavigateURL(StoreSettings.Current.PaymentTabId, "", param);
                param[1] = "status=0";
                var returnCancelUrl = Globals.NavigateURL(StoreSettings.Current.PaymentTabId, "", param);

                input.AmountInput = orderTotal;
                input.CurrencyInput = settings.GetXmlProperty("genxml/textbox/currencycode");
                input.MerchantReference = settings.GetXmlProperty("genxml/textbox/merchantref");
                input.TxnType = "Purchase";
                input.UrlFail = returnUrl;
                input.UrlSuccess = returnCancelUrl;

                input.TxnId = Guid.NewGuid().ToString().Substring(0, 16);
                orderData.PurchaseInfo.SetXmlProperty("genxml/txnid", input.TxnId);
                orderData.Save();

                RequestOutput output = WS.GenerateRequest(input);

                if (output.valid == "1" && output.URI != null)
                {
                    // Redirect user to payment page
                    return output.Url;
                }
                else
                {
                    // rollback transaction
                    orderData.PurchaseInfo.SetXmlProperty("genxml/paymenterror", "<div>PAYMENT RETURN ERROR: </div><div>" + output + "</div>");
                    orderData.PaymentFail();
                    return Globals.NavigateURL(StoreSettings.Current.PaymentTabId, "", param);
                }

            }
            catch (Exception ex)
            {
                // rollback transaction
                orderData.PurchaseInfo.SetXmlProperty("genxml/paymenterror", "<div>PAYMENT EXCEPTION: </div><div>" + ex + "</div>");
                orderData.PaymentFail();

                var param = new string[3];
                param[0] = "orderid=" + orderData.PurchaseInfo.ItemID.ToString("");
                param[1] = "status=0";
                return Globals.NavigateURL(StoreSettings.Current.PaymentTabId, "", param);
            }
        }

        public override string ProcessPaymentReturn(HttpContext context)
        {

            var ResultQs = Utils.RequestQueryStringParam(context, "result");

            if (!string.IsNullOrEmpty(ResultQs))
            {
                var orderid = Utils.RequestQueryStringParam(context, "orderid");
                if (Utils.IsNumeric(orderid))
                {
                    var settings = ProviderUtils.GetProviderSettings("NBrightPxPaypayment");

                    string PxPayUserId = settings.GetXmlProperty("genxml/textbox/pxpayuserid");
                    string PxPayKey = settings.GetXmlProperty("genxml/textbox/pxpaykey");

                    // Obtain the transaction result
                    PxPay WS = new PxPay(PxPayUserId, PxPayKey);

                    ResponseOutput output = WS.ProcessResponse(ResultQs);

                    if (output.TxnId != null)
                    {
                        var orderData = new OrderData(Convert.ToInt32(orderid));
                            // check we have a waiting for bank status.
                        if (orderData.OrderStatus == "020")
                        {
                            if (output.valid == "1" && output.Success == "1")
                            {
                                if (orderData.PurchaseInfo.GetXmlProperty("genxml/txnid") == output.TxnId)
                                {
                                    orderData.PaymentOk();
                                }
                            }
                            else
                            {
                                // update order fail
                                var rtnerr = orderData.PurchaseInfo.GetXmlProperty("genxml/paymenterror");
                                if (rtnerr == "") rtnerr = "fail"; // to return this so a fail is activated.
                                orderData.PaymentFail();
                                return rtnerr;
                            }
                        }
                    }
                }
            }
            return "";
        }
    }
}
