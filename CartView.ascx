<%@ Control language="C#" Inherits="Nevoweb.DNN.NBrightBuy.CartView" AutoEventWireup="true"  Codebehind="CartView.ascx.cs" %>
<asp:Repeater ID="checkoutlayout" runat="server" OnItemCommand="CtrlItemCommand"></asp:Repeater>
<asp:Repeater ID="rpDataH" runat="server" OnItemCommand="CtrlItemCommand"></asp:Repeater>
<asp:Repeater ID="rpData" runat="server" OnItemCommand="CtrlItemCommand"></asp:Repeater>
<asp:Repeater ID="rpDataF" runat="server" OnItemCommand="CtrlItemCommand"></asp:Repeater>
<asp:Repeater ID="rpAddrB" runat="server" OnItemCommand="CtrlItemCommand"></asp:Repeater>
<asp:Repeater ID="rpAddrS" runat="server" OnItemCommand="CtrlItemCommand"></asp:Repeater>
<asp:Repeater ID="rpShip" runat="server" OnItemCommand="CtrlItemCommand"></asp:Repeater>
<asp:Repeater ID="rpExtra" runat="server" OnItemCommand="CtrlItemCommand"></asp:Repeater>