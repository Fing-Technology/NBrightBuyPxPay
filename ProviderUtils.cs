using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using DotNetNuke.Entities.Portals;
using NBrightCore.common;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Components;

namespace Nevoweb.DNN.NBrightStore
{
    public class ProviderUtils
    {


        public static String GetTemplateData(String templatename, NBrightInfo pluginInfo)
        {
            var controlMapPath = HttpContext.Current.Server.MapPath("/DesktopModules/NBright/NBrightPxPay");
            var templCtrl = new NBrightCore.TemplateEngine.TemplateGetter(PortalSettings.Current.HomeDirectoryMapPath, controlMapPath, "Themes\\config", "");
            var templ = templCtrl.GetTemplateData(templatename, Utils.GetCurrentCulture());
            templ = Utils.ReplaceSettingTokens(templ, pluginInfo.ToDictionary());
            templ = Utils.ReplaceUrlTokens(templ);
            return templ;
        }

        public static NBrightInfo GetProviderSettings(String ctrlkey)
        {
            var info = (NBrightInfo)Utils.GetCache("NBrightPxPayPaymentProvider" + PortalSettings.Current.PortalId.ToString(""));
            if (info == null)
            {
                var modCtrl = new NBrightBuyController();

                info = modCtrl.GetByGuidKey(PortalSettings.Current.PortalId, -1, "NBrightPxPayPAYMENT", ctrlkey);

                if (info == null)
                {
                    info = new NBrightInfo(true);
                    info.GUIDKey = ctrlkey;
                    info.TypeCode = "NBrightPxPayPAYMENT";
                    info.ModuleId = -1;
                    info.PortalId = PortalSettings.Current.PortalId;
                }

                Utils.SetCache("NBrightPxPayPaymentProvider" + PortalSettings.Current.PortalId.ToString(""), info);
            }

            return info;
        }

        public static String GetBankRemotePost(OrderData orderData)
        {
            var rPost = new RemotePost();

            var settings = ProviderUtils.GetProviderSettings("NBrightPxPaypayment");

            var payData = new PayData(orderData);

            rPost.Url = payData.PostUrl;

            rPost.Add("param", "param");


            //Build the re-direct html 
            var rtnStr = rPost.GetPostHtml("/DesktopModules/NBright/NBrightPxPay/Themes/config/img/cic.jpg");
            if (settings.GetXmlPropertyBool("genxml/checkbox/debugmode"))
            {
                File.WriteAllText(PortalSettings.Current.HomeDirectoryMapPath + "\\debug_NBrightPxPaypost.html", rtnStr);
            }
            return rtnStr;
        }


    }
}
