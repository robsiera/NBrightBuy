<%@ Control Language="C#" AutoEventWireup="false" Inherits="DotNetNuke.UI.Skins.Skin" %>
<%@ Register TagPrefix="nbs" TagName="BACKOFFICE" Src="~/DesktopModules/NBright/NBrightBuy/Admin/BackOffice.ascx" %>
<%@ Register TagPrefix="nbs" TagName="MENU" Src="~/DesktopModules/NBright/NBrightBuy/Admin/Menu.ascx" %>
<style type="text/css">#ControlBar_ControlPanel,.dnnDragHint,.actionMenu,.dnnActionMenuBorder,.dnnActionMenu{display:none !important}#Form.showControlBar{margin-top:0 !important}.dnnEditState .DnnModule,.DnnModule{opacity:1 !important}</style>
<div id="ControlPanel" runat="server" visible="false"></div>
<div id="ContentPane" class="" runat="server"></div>
<div>
<div style="float:left;width:300px;">
<nbs:MENU id="nbsMenu" runat="server" />
</div>
<div  style="float:left;">
<nbs:BACKOFFICE id="nbsBackOffice" runat="server" />
</div>
<div style="clear:both;"></div>
</div>



