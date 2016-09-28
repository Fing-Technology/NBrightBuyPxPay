# NBrightBuyPxPay
Payment Gateway for Payment Express

NOTE: By default the PxPay return url passes a pxpay "userid" parameter.  This is incorrectly picked up by the DNN Breadcrumb control as the DNN userid, this then causes an error.  To stop this error your return skin for the payment page must NOT include the standard DNN breadcrumb skin control.
