﻿using System;
using System.Xml;
using DotNetNuke.Entities.Portals;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using System.Web.UI.WebControls;

namespace Nevoweb.DNN.NBrightBuy.Providers
{
    public class CartProvider : Nevoweb.DNN.NBrightBuy.Components.Interfaces.CartInterface  
    {

        public override NBrightInfo ValidateCart(NBrightInfo cartInfo)
        {


            return cartInfo;
        }
    }
}
