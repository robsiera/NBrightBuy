﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting;
using System.Web;
using System.Web.UI.WebControls;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using NBrightCore.render;
using NBrightDNN;
using NBrightDNN.controls;
using NBrightCore.common;
using System.Xml;
using Nevoweb.DNN.NBrightBuy.Components;

namespace Nevoweb.DNN.NBrightBuy.Base
{
    public class NBrightBuyBase : DotNetNuke.Entities.Modules.PortalModuleBase
	{
        protected NBrightCore.controls.PagingCtrl CtrlPaging;
        public NBrightBuyController ModCtrl;
		public bool DebugMode = false;
		public string ModuleKey = "";
		public string ModuleAppType = "1";
		public string UploadFolder = "";
		public string SelUserId = "";
        public string ThemeFolder = "";
	    public ModSettings ModSettings;
        public string RazorTemplate = "";

        public Boolean EnablePaging;

        public DotNetNuke.Framework.CDefault BasePage
        {
            get { return (DotNetNuke.Framework.CDefault) this.Page; }
        }

		protected override void OnInit(EventArgs e)
		{

            ModCtrl = new NBrightBuyController();
            DebugMode = StoreSettings.Current.DebugMode;

		    base.OnInit(e);

            #region "Get all Settings for module"
            //get Model Level Settings
            ModSettings = new ModSettings(ModuleId, Settings);
            ModuleKey = ModSettings.Get("modref");
            if (String.IsNullOrEmpty(ModuleKey)) ModuleKey = ModSettings.Get("modulekey"); // keep backward compatiblity with NBS_ProductView.

            #endregion

            if (EnablePaging)
            {
                CtrlPaging = new NBrightCore.controls.PagingCtrl();
                this.Controls.Add(CtrlPaging);
                CtrlPaging.PageChanged += new RepeaterCommandEventHandler(PagingClick);
            }

            //add template provider to NBright Templating
            NBrightCore.providers.GenXProviderManager.AddProvider("NBrightBuy,Nevoweb.DNN.NBrightBuy.render.GenXmlTemplateExt");
          
            // search for any other NBright Tenmplating providers that might have been added.
            var pluginData = new PluginData(PortalSettings.Current.PortalId);
            var l = pluginData.GetTemplateExtProviders();
            foreach (var p in l)
            {
                var prov = p.Value;
                NBrightCore.providers.GenXProviderManager.AddProvider(prov.GetXmlProperty("genxml/textbox/assembly") + "," + prov.GetXmlProperty("genxml/textbox/namespaceclass"));
            }

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // load the notificationmessage if with have a placeholder control to display it.
            var ctrlMsg = this.FindControl("notifymsg");
            if (ctrlMsg != null)
            {
                var msg = NBrightBuyUtils.GetNotfiyMessage(ModuleId);
                var l = new Literal {Text = msg};
                ctrlMsg.Controls.Add(l);
            }
        }




        #region "Display Methods"


        public void DoDetail(Repeater rp1, NBrightInfo obj)
        {
            var l = new List<object> { obj };
            rp1.DataSource = l;
            rp1.DataBind();
        }

        public void DoDetail(Repeater rp1)
        {
            DoDetail(rp1,0);
        }

        /// <summary>
        /// Display template with moduleid set
        /// </summary>
        /// <param name="rp1"></param>
        /// <param name="moduleId"></param>
        public void DoDetail(Repeater rp1,int moduleId) 
        {
            var obj = new NBrightInfo(true);
            obj.ModuleId = moduleId;
            var l = new List<object> { obj };
            rp1.DataSource = l;
            rp1.DataBind();
        }

        #endregion

        #region "Events"


        protected virtual void PagingClick(object source, RepeaterCommandEventArgs e)
        {
            var cArg = e.CommandArgument.ToString();
            EventBeforePageChange(source, e);
        }

        public virtual void EventBeforePageChange(object source, RepeaterCommandEventArgs e)
        {

        }

        #endregion


    }
}
