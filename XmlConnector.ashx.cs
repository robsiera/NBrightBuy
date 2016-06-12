﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Xml;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using Microsoft.ApplicationBlocks.Data;
using Microsoft.SqlServer.Server;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Components;
using DataProvider = DotNetNuke.Data.DataProvider;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Services.Exceptions;
using NBrightBuy.render;
using NBrightCore.images;
using Nevoweb.DNN.NBrightBuy.Components.Interfaces;

namespace Nevoweb.DNN.NBrightBuy
{
    /// <summary>
    /// Summary description for XMLconnector
    /// </summary>
    public class XmlConnector : IHttpHandler
    {
        private readonly JavaScriptSerializer _js = new JavaScriptSerializer();
        private String _lang = "";

        public void ProcessRequest(HttpContext context)
        {
            #region "Initialize"

            var strOut = "";

            var paramCmd = Utils.RequestQueryStringParam(context, "cmd");
            var itemId = Utils.RequestQueryStringParam(context, "itemid");
            var ctlType = Utils.RequestQueryStringParam(context, "ctltype");
            var idXref = Utils.RequestQueryStringParam(context, "idxref");
            var xpathpdf = Utils.RequestQueryStringParam(context, "pdf");
            var xpathref = Utils.RequestQueryStringParam(context, "pdfref");
            var lang = Utils.RequestQueryStringParam(context, "lang");
            var language = Utils.RequestQueryStringParam(context, "language");
            var moduleId = Utils.RequestQueryStringParam(context, "mid");
            var moduleKey = Utils.RequestQueryStringParam(context, "mkey");
            var parentid = Utils.RequestQueryStringParam(context, "parentid");
            var entryid = Utils.RequestQueryStringParam(context, "entryid");
            var entryxid = Utils.RequestQueryStringParam(context, "entryxid");
            var catid = Utils.RequestQueryStringParam(context, "catid");
            var catxid = Utils.RequestQueryStringParam(context, "catxid");
            var templatePrefix = Utils.RequestQueryStringParam(context, "tprefix");
            var value = Utils.RequestQueryStringParam(context, "value");
            var itemListName = Utils.RequestQueryStringParam(context, "listname");
            if (itemListName == "") itemListName = "ItemList";
            if (itemListName == "*") itemListName = "ItemList";

            #region "setup language"

            // because we are using a webservice the system current thread culture might not be set correctly,
            SetContextLangauge(context);

            #endregion

            #endregion

            #region "Do processing of command"

            var intModuleId = 0;
            if (Utils.IsNumeric(moduleId)) intModuleId = Convert.ToInt32(moduleId);

            var objCtrl = new NBrightBuyController();

            var uInfo = new UserDataInfo(UserController.GetCurrentUserInfo().PortalID, intModuleId, objCtrl, ctlType);
            strOut = "ERROR!! - No Security rights for current user!";
            switch (paramCmd)
            {
                case "test":
                    strOut = "<root>" + UserController.GetCurrentUserInfo().Username + "</root>";
                    break;
                case "setdata":
                    break;
                case "deldata":
                    break;
                //case "setcategoryadminform":
                //    if (NBrightBuyUtils.CheckRights()) strOut = SetCategoryForm(context);
                //    break;
                case "getdata":
                    strOut = GetReturnData(context);
                    break;
                case "additemlist":
                    if (Utils.IsNumeric(itemId))
                    {
                        var cw = new ItemListData(itemListName);
                        cw.Add(itemId);
                        strOut = cw.ItemList;
                    }
                    break;
                case "removeitemlist":
                    if (Utils.IsNumeric(itemId))
                    {
                        var cw1 = new ItemListData(itemListName);
                        cw1.Remove(itemId);
                        strOut = cw1.ItemList;
                    }
                    break;
                case "deleteitemlist":
                        var cw2 = new ItemListData(itemListName);
                        cw2.Delete();
                        strOut = "deleted";
                    break;
                case "getproductselectlist":
                    strOut = GetProductList(context);
                    break;
                case "getproductlist":
                    strOut = GetProductList(context);
                    break;
                case "getcategoryproductlist":
                    strOut = GetCategoryProductList(context);
                    break;
                case "setdefaultcategory":
                    if (NBrightBuyUtils.CheckRights()) strOut = SetDefaultCategory(context);
                    break;
                case "deletecatxref":
                    if (NBrightBuyUtils.CheckRights()) strOut = DeleteCatXref(context);
                    break;
                case "selectcatxref":
                    if (NBrightBuyUtils.CheckRights()) strOut = SelectCatXref(context);
                    break;
                case "deleteallcatxref":
                    if (NBrightBuyUtils.CheckRights()) strOut = DeleteAllCatXref(context);
                    break;
                case "copyallcatxref":
                    if (NBrightBuyUtils.CheckRights()) strOut = CopyAllCatXref(context);
                    break;
                case "moveallcatxref":
                    if (NBrightBuyUtils.CheckRights()) strOut = CopyAllCatXref(context,true);
                    break;
                case "editproduct":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetProductGeneralData(context);
                    break;
                case "productdescription":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetProductDescription(context);
                    break;
                case "productmodels":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetProductModels(context);
                    break;
                case "productoptions":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetProductOptions(context);
                    break;
                case "productoptionvalues":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetProductOptionValues(context);
                    break;
                case "productimages":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetProductImages(context);
                    break;
                case "productdocs":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetProductDocs(context);
                    break;
                case "productrelatedproducts":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetProductModels(context);
                    break;
                case "productcategories":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetProductCategories(context);
                    break;
                case "productisincategory":
                    if (NBrightBuyUtils.CheckRights()) strOut = ProductIsInCategory(context).ToString();
                    break;
                case "productgroupcategories":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetProductGroupCategories(context);
                    break;                    
                case "productrelated":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetProductRelated(context);
                    break;
                case "productclients":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetProductClients(context);
                    break;
                case "getclientselectlist":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetClientSelectList(context);
                    break;
                case "addproductmodels":
                    if (NBrightBuyUtils.CheckRights()) strOut = AddProductModels(context);
                    break;
                case "addproductoptions":
                    if (NBrightBuyUtils.CheckRights()) strOut = AddProductOptions(context);
                    break;
                case "addproductoptionvalues":
                    if (NBrightBuyUtils.CheckRights()) strOut = AddProductOptionValues(context);
                    break;
                case "addproductcategory":
                    if (NBrightBuyUtils.CheckRights()) strOut = AddProductCategory(context);
                    break;
                case "addproductgroupcategory":
                    if (NBrightBuyUtils.CheckRights()) strOut = AddProductGroupCategory(context);
                    break;
                case "removeproductcategory":
                    if (NBrightBuyUtils.CheckRights()) strOut = RemoveProductCategory(context);
                    break;
                case "removeproductgroupcategory":
                    if (NBrightBuyUtils.CheckRights()) strOut = RemoveProductGroupCategory(context);
                    break;                    
                case "populatecategorylist":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetGroupCategoryListBox(context);
                    break;
                case "addrelatedproduct":
                    if (NBrightBuyUtils.CheckRights()) strOut = AddRelatedProduct(context);
                    break;
                case "removerelatedproduct":
                    if (NBrightBuyUtils.CheckRights()) strOut = RemoveRelatedProduct(context);
                    break;
                case "addproductclient":
                    if (NBrightBuyUtils.CheckRights()) strOut = AddProductClient(context);
                    break;
                case "removeproductclient":
                    if (NBrightBuyUtils.CheckRights()) strOut = RemoveProductClient(context);
                    break;
                case "clientdiscountcodes":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetClientDiscountCodes(context);
                    break;
                case "addclientdiscountcode":
                    if (NBrightBuyUtils.CheckRights()) strOut = AddClientDiscountCodes(context);
                    break;
                case "clientvouchercodes":
                    if (NBrightBuyUtils.CheckRights()) strOut = GetClientVoucherCodes(context);
                    break;
                case "addclientvouchercode":
                    if (NBrightBuyUtils.CheckRights()) strOut = AddClientVoucherCodes(context);
                    break;
                case "moveproductadmin":
                    if (NBrightBuyUtils.CheckRights()) strOut = MoveProductAdmin(context);
                    break;
                case "fileupload":
                    if (NBrightBuyUtils.CheckRights() && Utils.IsNumeric(itemId))
                    {
                        strOut = FileUpload(context);
                    }
                    break;
                case "updateproductimages":
                    if (NBrightBuyUtils.CheckRights())
                    {
                        UpdateProductImages(context);
                        strOut = GetProductImages(context);
                    }
                    break;
                case "updateproductdocs":
                    if (NBrightBuyUtils.CheckRights())
                    {
                        UpdateProductDocs(context);
                        strOut = GetProductDocs(context);
                    }
                    break;
                case "addtobasket":
                        strOut = AddToBasket(context);
                    break;
                case "addalltobasket":
                    strOut = AddAllToBasket(context);
                    break;
                case "addcookietobasket":
                    break;
                case "docdownload":
                    var fname = Utils.RequestQueryStringParam(context, "filename");
                    strOut = fname; // return this is error.
                    var downloadname = Utils.RequestQueryStringParam(context, "downloadname");
                    var fpath = HttpContext.Current.Server.MapPath(fname);
                    if (downloadname == "") downloadname = Path.GetFileName(fname);
                    Utils.ForceDocDownload(fpath, downloadname, context.Response);
                    break;
                case "printproduct":
                    break;
                case "removefromcart":
                    RemoveFromCart(context);
                    strOut = "removefromcart";
                    break;
                case "recalculatecart":
                    RecalculateCart(context);
                    strOut = "recalculatecart";
                    break;
                case "recalculatesummary":
                    RecalculateSummary(context);
                    strOut = "recalculatecart";
                    break;
                case "redirecttopayment":
                    strOut = RedirectToPayment(context);
                    break;
                case "updatebilladdress":
                    strOut = UpdateCartAddress(context,"bill");
                    break;
                case "updateshipaddress":
                    strOut = UpdateCartAddress(context, "ship");
                    break;
                case "updateshipoption":
                    strOut = UpdateCartAddress(context, "shipoption");
                    break;
                case "rendercart":
                        strOut = RenderCart(context);
                    break;
                case "renderpostdata":
                    strOut = RenderPostData(context);
                    break;
                case "clearcart":
                    var currentcart = new CartData(PortalSettings.Current.PortalId);
                    currentcart.DeleteCart();
                    strOut = "clearcart";
                    break;
                case "shippingprovidertemplate":
                    strOut = GetShippingProviderTemplates(context);
                    break;                    
                case "getsettings":
                    strOut = GetSettings(context);
                    break;
                case "savesettings":
                    if (NBrightBuyUtils.CheckRights()) strOut = SaveSettings(context);
                    break;
                case "updateprofile":
                    strOut = UpdateProfile(context);
                    break;
            }

            #endregion

            #region "return results"

            //send back xml as plain text
            context.Response.Clear();
                context.Response.ContentType = "text/plain";
                context.Response.Write(strOut);
                context.Response.End();                

            #endregion

        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }


        private void SetContextLangauge(HttpContext context)
        {
            var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
            SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.
        }

        private void SetContextLangauge(NBrightInfo ajaxInfo = null)
        {
            // NOTE: "genxml/hidden/lang" should be set in the template for langauge to work OK.
            // set langauge if we have it passed.
            if (ajaxInfo == null) ajaxInfo = new NBrightInfo(true);
            var lang = ajaxInfo.GetXmlProperty("genxml/hidden/currentlang");
            if (lang == "") lang = Utils.RequestParam(HttpContext.Current, "langauge"); // fallbacl
            if (lang == "") lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang"); // fallbacl
            if (lang == "") lang = Utils.GetCurrentCulture(); // fallback, but very often en-US on ajax call
            // set the context  culturecode, so any DNN functions use the correct culture 
            if (lang != "" && lang != System.Threading.Thread.CurrentThread.CurrentCulture.ToString()) System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(lang);

        }


        #region "fileupload"

        private void UpdateProductImages(HttpContext context)
        {
            //get uploaded params
            var settings = GetAjaxFields(context);
            if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
            var productitemid = settings["itemid"];
            var imguploadlist = settings["imguploadlist"];

            if (Utils.IsNumeric(productitemid))
            {
                var imgs = imguploadlist.Split(',');
                foreach (var img in imgs)
                {
                    if (ImgUtils.IsImageFile(Path.GetExtension(img)) && img != "")
                    {
                        string fullName = StoreSettings.Current.FolderTempMapPath + "\\" + img;
                        if (File.Exists(fullName))
                        {
                            var imgResize = StoreSettings.Current.GetInt(StoreSettingKeys.productimageresize);
                            if (imgResize == 0) imgResize = 800;
                            var imagepath = ResizeImage(fullName, imgResize);
                            var imageurl = StoreSettings.Current.FolderImages.TrimEnd('/') + "/" + Path.GetFileName(imagepath);
                            AddNewImage(Convert.ToInt32(productitemid), imageurl, imagepath);                                                    
                        }
                    }
                }
                // clear any cache for the product.
                ProductUtils.RemoveProductDataCache(Convert.ToInt32(productitemid), _lang);
                var cachekey = "AjaxProductImgs*" + productitemid;
                Utils.RemoveCache(cachekey);

            }
        }

        private String ResizeImage(String fullName, int imgSize = 640)
        {
            if (ImgUtils.IsImageFile(Path.GetExtension(fullName)))
            {
                var extension = Path.GetExtension(fullName);
                var newImageFileName = StoreSettings.Current.FolderImagesMapPath.TrimEnd(Convert.ToChar("\\")) + "\\" + Utils.GetUniqueKey() + extension;
                if (extension != null && extension.ToLower() == ".png")
                {
                    newImageFileName = ImgUtils.ResizeImageToPng(fullName, newImageFileName, imgSize);
                }
                else
                {
                    newImageFileName = ImgUtils.ResizeImageToJpg(fullName, newImageFileName, imgSize);
                }
                Utils.DeleteSysFile(fullName);

                return newImageFileName;

            }
            return "";
        }


        private void AddNewImage(int itemId,String imageurl, String imagepath)
        {
            var objCtrl = new NBrightBuyController();
            var dataRecord = objCtrl.Get(itemId);
            if (dataRecord != null)
            {
                var strXml = "<genxml><imgs><genxml><hidden><imagepath>" + imagepath + "</imagepath><imageurl>" + imageurl + "</imageurl></hidden></genxml></imgs></genxml>";
                if (dataRecord.XMLDoc.SelectSingleNode("genxml/imgs") == null)
                {
                    dataRecord.AddXmlNode(strXml, "genxml/imgs", "genxml");
                }
                else
                {
                    dataRecord.AddXmlNode(strXml, "genxml/imgs/genxml", "genxml/imgs");
                }
                objCtrl.Update(dataRecord);
            }
        }


        private string FileUpload(HttpContext context)
        {
            try
            {

                var strOut = "";
                switch (context.Request.HttpMethod)
                {
                    case "HEAD":
                    case "GET":
                        break;
                    case "POST":
                    case "PUT":
                        strOut = UploadFile(context);
                        break;
                    case "DELETE":
                        break;
                    case "OPTIONS":
                        break;

                    default:
                        context.Response.ClearHeaders();
                        context.Response.StatusCode = 405;
                        break;
                }

                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            
        }

        // Upload file to the server
        private String UploadFile(HttpContext context)
        {
            var statuses = new List<FilesStatus>();
            var headers = context.Request.Headers;

            if (string.IsNullOrEmpty(headers["X-File-Name"]))
            {
                return UploadWholeFile(context, statuses);
            }
            else
            {
                return UploadPartialFile(headers["X-File-Name"], context, statuses);
            }
        }

        // Upload partial file
        private String UploadPartialFile(string fileName, HttpContext context, List<FilesStatus> statuses)
        {
            if (context.Request.Files.Count != 1) throw new HttpRequestValidationException("Attempt to upload chunked file containing more than one fragment per request");
            var inputStream = context.Request.Files[0].InputStream;
            var fullName = StoreSettings.Current.FolderTempMapPath + "\\" + fileName;

            using (var fs = new FileStream(fullName, FileMode.Append, FileAccess.Write))
            {
                var buffer = new byte[1024];

                var l = inputStream.Read(buffer, 0, 1024);
                while (l > 0)
                {
                    fs.Write(buffer, 0, l);
                    l = inputStream.Read(buffer, 0, 1024);
                }
                fs.Flush();
                fs.Close();
            }
            statuses.Add(new FilesStatus(new FileInfo(fullName)));
            return "";
        }

        // Upload entire file
        private String UploadWholeFile(HttpContext context, List<FilesStatus> statuses)
        {
            for (int i = 0; i < context.Request.Files.Count; i++)
            {
                var file = context.Request.Files[i];
                file.SaveAs(StoreSettings.Current.FolderTempMapPath + "\\" + file.FileName);
                statuses.Add(new FilesStatus(Path.GetFileName(file.FileName), file.ContentLength));
            }
            return "";
        }

        private void WriteJsonIframeSafe(HttpContext context, List<FilesStatus> statuses)
        {
            context.Response.AddHeader("Vary", "Accept");
            try
            {
                if (context.Request["HTTP_ACCEPT"].Contains("application/json"))
                    context.Response.ContentType = "application/json";
                else
                    context.Response.ContentType = "text/plain";
            }
            catch
            {
                context.Response.ContentType = "text/plain";
            }

            var jsonObj = _js.Serialize(statuses.ToArray());
            context.Response.Write(jsonObj);
        }



        #endregion

        #region "SQL Data return"

        private string GetReturnData(HttpContext context)
        {
            try
            {

                var strOut = "";

                var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
                var xmlData = GenXmlFunctions.GetGenXmlByAjax(strIn, "");
                var objInfo = new NBrightInfo();

                objInfo.ItemID = -1;
                objInfo.TypeCode = "AJAXDATA";
                objInfo.XMLData = xmlData;
                var settings = objInfo.ToDictionary();

                var themeFolder = StoreSettings.Current.ThemeFolder;
                if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);

                if (!settings.ContainsKey("portalid")) settings.Add("portalid", PortalSettings.Current.PortalId.ToString("")); // aways make sure we have portalid in settings
                var objCtrl = new NBrightBuyController();

                // run SQL and template to return html
                if (settings.ContainsKey("sqltpl") && settings.ContainsKey("xsltpl"))
                {
                    var strSql = templCtrl.GetTemplateData(settings["sqltpl"], _lang, true, true, true, StoreSettings.Current.Settings());
                    var xslTemp = templCtrl.GetTemplateData(settings["xsltpl"], _lang, true, true, true, StoreSettings.Current.Settings());

                    // replace any settings tokens (This is used to place the form data into the SQL)
                    strSql = Utils.ReplaceSettingTokens(strSql, settings);
                    strSql = Utils.ReplaceUrlTokens(strSql);

                    strSql = GenXmlFunctions.StripSqlCommands(strSql); // don't allow anything to update through here.

                    strOut = objCtrl.GetSqlxml(strSql);
                    if (!strOut.StartsWith("<root>")) strOut = "<root>" + strOut + "</root>"; // always wrap with root node.
                    strOut = XslUtils.XslTransInMemory(strOut, xslTemp);
                }

                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }


        #endregion

        #region "Category Methods"

        private String GetCategoryProductList(HttpContext context)
        {
            try
            {
                var objQual = DotNetNuke.Data.DataProvider.Instance().ObjectQualifier;
                var dbOwner = DataProvider.Instance().DatabaseOwner;

                var settings = GetAjaxFields(context);
                var strFilter = " and NB1.[ItemId] in (select parentitemid from " + dbOwner + "[" + objQual + "NBrightBuy] where typecode = 'CATXREF' and XrefItemId = {Settings:itemid}) ";

                strFilter = Utils.ReplaceSettingTokens(strFilter, settings);


                if (!settings.ContainsKey("filter")) settings.Add("filter", strFilter);
                return GetProductListData(settings);

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }


        }

        private string DeleteCatXref(HttpContext context)
        {
            try
            {
                var settings = GetAjaxFields(context);
                var parentitemid = "";
                var xrefitemid = "";
                if (settings.ContainsKey("parentitemid")) parentitemid = settings["parentitemid"];
                if (settings.ContainsKey("xrefitemid")) xrefitemid = settings["xrefitemid"];
                if (Utils.IsNumeric(xrefitemid) && Utils.IsNumeric(parentitemid))
                {
                    DeleteCatXref(xrefitemid, parentitemid);
                }
                else
                    return "Invalid parentitemid or xrefitemid";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
            return "";
        }

        private void DeleteCatXref(String xrefitemid, String parentitemid)
        {
            if (Utils.IsNumeric(xrefitemid) && Utils.IsNumeric(parentitemid))
            {
                var prodData = ProductUtils.GetProductData(Convert.ToInt32(parentitemid), _lang, false);
                prodData.RemoveCategory(Convert.ToInt32(xrefitemid));
            }
        }

        private string SelectCatXref(HttpContext context)
        {
            try
            {
                var settings = GetAjaxFields(context);
                var parentitemid = "";
                var xrefitemid = "";
                if (settings.ContainsKey("parentitemid")) parentitemid = settings["parentitemid"];
                if (settings.ContainsKey("xrefitemid")) xrefitemid = settings["xrefitemid"];
                if (Utils.IsNumeric(xrefitemid) && Utils.IsNumeric(parentitemid))
                {
                    var prodData = ProductUtils.GetProductData(Convert.ToInt32(parentitemid), _lang, false);
                    prodData.AddCategory(Convert.ToInt32(xrefitemid));
                }
                else
                    return "Invalid parentitemid or xrefitmeid";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
            return "";
        }

        private string DeleteAllCatXref(HttpContext context)
        {
            var strOut = NBrightBuyUtils.GetResxMessage("general_fail");
            try
            {
                var settings = GetAjaxFields(context);

                if (settings.ContainsKey("itemid"))
                {
                    var strFilter = " and XrefItemId = {Settings:itemid} ";
                    strFilter = Utils.ReplaceSettingTokens(strFilter, settings);

                    var objCtrl = new NBrightBuyController();
                    var objList = objCtrl.GetList(PortalSettings.Current.PortalId, -1, "CATXREF", strFilter);

                    foreach (var obj in objList)
                    {
                        DeleteCatXref(settings["itemid"], obj.ParentItemId.ToString(""));
                    }
                    strOut = NBrightBuyUtils.GetResxMessage();
                }
            }
            catch (Exception e)
            {
                return e.ToString();
            }
            return strOut;
        }

        private String CopyAllCatXref(HttpContext context,Boolean moverecords = false)
        {
            var strOut = NBrightBuyUtils.GetResxMessage("general_fail");
            try
            {
                var settings = GetAjaxFields(context);
                var strFilter = " and XrefItemId = {Settings:itemid} ";

                strFilter = Utils.ReplaceSettingTokens(strFilter, settings);

                var newcatid = "";
                if (settings.ContainsKey("selectedcatid")) newcatid = settings["selectedcatid"];

                if (Utils.IsNumeric(newcatid) && settings.ContainsKey("itemid"))
                {
                    var objCtrl = new NBrightBuyController();
                    var objList = objCtrl.GetList(PortalSettings.Current.PortalId, -1, "CATXREF", strFilter);

                    foreach (var obj in objList)
                    {
                        var strGuid = newcatid + "x" + obj.ParentItemId.ToString("");
                        var nbi = objCtrl.GetByGuidKey(PortalSettings.Current.PortalId, -1, "CATXREF", strGuid);
                        if (nbi == null)
                        {
                            if (!moverecords) obj.ItemID = -1;
                            obj.XrefItemId = Convert.ToInt32(newcatid);
                            obj.GUIDKey = strGuid;
                            obj.XMLData = null;
                            obj.TextData = null;
                            obj.Lang = null;
                            objCtrl.Update(obj);
                            //add all cascade xref 
                            var objGrpCtrl = new GrpCatController(_lang, true);
                            var parentcats = objGrpCtrl.GetCategory(Convert.ToInt32(newcatid));
                            foreach (var p in parentcats.Parents)
                            {
                                strGuid = p.ToString("") + "x" + obj.ParentItemId.ToString("");
                                nbi = objCtrl.GetByGuidKey(PortalSettings.Current.PortalId, -1, "CATCASCADE", strGuid);
                                if (nbi == null)
                                {
                                    obj.XrefItemId = p;
                                    obj.TypeCode = "CATCASCADE";
                                    obj.GUIDKey = strGuid;
                                    objCtrl.Update(obj);
                                }
                            }
                        }
                    }

                    if (moverecords) DeleteAllCatXref(context);

                    strOut = NBrightBuyUtils.GetResxMessage();
                }

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            return strOut;
        }

        private String GetGroupCategoryListBox(HttpContext context)
        {
            var settings = GetAjaxFields(context);
            var groupref = "";
            if (settings.ContainsKey("selectedgroupref")) groupref = settings["selectedgroupref"];
            var templ = "[<tag id='selectgroupcategory' cssclass='selectgroupcategory form-control' type='catlistbox' groupref='" + groupref + "' lang='" + _lang + "'/>]";
            return GenXmlFunctions.RenderRepeater(new NBrightInfo(),templ);    
        }

        #endregion

        #region "Product Methods"

        private String GetProductList(HttpContext context)
        {
            try
            {

                var settings = GetAjaxFields(context);

                return GetProductListData(settings);

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }


        }

        private String MoveProductAdmin(HttpContext context)
        {
            try
            {

                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("moveproductid")) settings.Add("moveproductid", "0");
                var moveproductid = settings["moveproductid"];
                if (!settings.ContainsKey("movetoproductid")) settings.Add("movetoproductid", "0");
                var movetoproductid = settings["movetoproductid"];
                if (!settings.ContainsKey("searchcategory")) settings.Add("searchcategory", "0");
                var searchcategory = settings["searchcategory"];

                var objCtrl = new NBrightBuyController();
                objCtrl.GetListCustom(PortalSettings.Current.PortalId, -1, "NBrightBuy_MoveProductinCateogry", 0, "", searchcategory + ";" + moveproductid + ";" + movetoproductid);

                DataCache.ClearCache();

                return GetProductListData(settings);

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }


        }


        private String GetProductDescription(HttpContext context)
        {
            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                var productitemid = settings["itemid"];

                //get data
                var prodData = ProductUtils.GetProductData(productitemid, _lang);

                return  HttpUtility.HtmlDecode(prodData.Info.GetXmlProperty("genxml/lang/genxml/edt/description"));

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String GetProductGeneralData(HttpContext context)
        {
            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                var productitemid = settings["itemid"];
                
                // get template
                var themeFolder = StoreSettings.Current.ThemeFolder;
                if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                var bodyTempl = templCtrl.GetTemplateData("productadmingeneral.html", _lang, true, true, true, StoreSettings.Current.Settings());

                //get data
                var prodData = ProductUtils.GetProductData(productitemid, _lang);
                var strOut = GenXmlFunctions.RenderRepeater(prodData.Info, bodyTempl);

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            
        }
        
        private String GetProductModels(HttpContext context)
        {
            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                var productitemid = settings["itemid"];

                // get template
                var themeFolder = StoreSettings.Current.ThemeFolder;
                if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                var bodyTempl = templCtrl.GetTemplateData("productadminmodels.html", _lang, true, true, true, StoreSettings.Current.Settings());

                //get data
                var prodData = ProductUtils.GetProductData(productitemid, _lang);
                var strOut = GenXmlFunctions.RenderRepeater(prodData.Models, bodyTempl);

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String GetProductOptions(HttpContext context)
        {
            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                var productitemid = settings["itemid"];

                // get template
                var themeFolder = StoreSettings.Current.ThemeFolder;
                if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                var bodyTempl = templCtrl.GetTemplateData("productadminoptions.html", _lang, true, true, true, StoreSettings.Current.Settings());

                //get data
                var prodData = ProductUtils.GetProductData(productitemid, _lang);
                var strOut = GenXmlFunctions.RenderRepeater(prodData.Options, bodyTempl);

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String GetProductOptionValues(HttpContext context)
        {
            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                var productitemid = settings["itemid"];

                // get template
                var themeFolder = StoreSettings.Current.ThemeFolder;
                if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                var bodyTempl = templCtrl.GetTemplateData("productadminoptionvalues.html", _lang, true, true, true, StoreSettings.Current.Settings());

                //get data
                var strOut = "";
                if (Utils.IsNumeric(productitemid))
                {
                    var prodData = ProductUtils.GetProductData(productitemid, _lang);
                    strOut = GenXmlFunctions.RenderRepeater(prodData.OptionValues, bodyTempl);  
                }

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String GetProductImages(HttpContext context)
        {
            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                var productitemid = settings["itemid"];

                var cachekey = "AjaxProductImgs*" + productitemid.ToString();
                var cacheData = Utils.GetCache(cachekey);
                var strOut = "";
                if (cacheData == null)
                {
                    // get template
                    var themeFolder = StoreSettings.Current.ThemeFolder;
                    if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                    var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                    var bodyTempl = templCtrl.GetTemplateData("productadminimages.html", _lang, true, true, true, StoreSettings.Current.Settings());

                    //get data
                    var prodData = ProductUtils.GetProductData(productitemid, _lang);
                    strOut = GenXmlFunctions.RenderRepeater(prodData.Imgs, bodyTempl);                    
                }
                else
                {
                    strOut = cacheData.ToString();
                }

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private void UpdateProductDocs(HttpContext context)
        {
            //get uploaded params
            var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
            var settings = ajaxInfo.ToDictionary();

            if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
            var productitemid = settings["itemid"];
            var docuploadlist = settings["docuploadlist"];

            if (Utils.IsNumeric(productitemid))
            {
                var docs = docuploadlist.Split(',');
                foreach (var doc in docs)
                {
                    if (doc != "")
                    {
                        string fullName = StoreSettings.Current.FolderTempMapPath + "\\" + doc;
                        var extension = Path.GetExtension(fullName);
                        if ((extension.ToLower() == ".pdf" || extension.ToLower() == ".zip"))
                        {
                            if (File.Exists(fullName))
                            {
                                var newDocFileName = StoreSettings.Current.FolderDocumentsMapPath.TrimEnd(Convert.ToChar("\\")) + "\\" + Guid.NewGuid() + extension;
                                File.Copy(fullName, newDocFileName, true);
                                var docurl = StoreSettings.Current.FolderDocuments.TrimEnd('/') + "/" + Path.GetFileName(newDocFileName);
                                AddNewDoc(Convert.ToInt32(productitemid), newDocFileName, doc);
                            }
                        }
                    }
                }
                // clear any cache for the product.
                ProductUtils.RemoveProductDataCache(Convert.ToInt32(productitemid), _lang);

            }
        }

        private void AddNewDoc(int itemId, String filepath, String orginalfilename)
        {
            var objCtrl = new NBrightBuyController();
            var dataRecord = objCtrl.Get(itemId);
            if (dataRecord != null)
            {
                var fileext = Path.GetExtension(orginalfilename);
                var strXml = "<genxml><docs><genxml><hidden><filepath>" + filepath + "</filepath><fileext>" + fileext + "</fileext></hidden><textbox><txtfilename>" + orginalfilename + "</txtfilename></textbox></genxml></docs></genxml>";
                if (dataRecord.XMLDoc.SelectSingleNode("genxml/docs") == null)
                {
                    dataRecord.AddXmlNode(strXml, "genxml/docs", "genxml");
                }
                else
                {
                    dataRecord.AddXmlNode(strXml, "genxml/docs/genxml", "genxml/docs");
                }
                objCtrl.Update(dataRecord);
            }
        }



        private String GetProductDocs(HttpContext context)
        {
            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                var productitemid = settings["itemid"];

                // get template
                var themeFolder = StoreSettings.Current.ThemeFolder;
                if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                var bodyTempl = templCtrl.GetTemplateData("productadmindocs.html", _lang, true, true, true, StoreSettings.Current.Settings());

                //get data
                var prodData = ProductUtils.GetProductData(productitemid, _lang);
                var strOut = GenXmlFunctions.RenderRepeater(prodData.Docs, bodyTempl);

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private Boolean ProductIsInCategory(HttpContext context)
        {
            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                var productitemid = settings["itemid"];
                if (!settings.ContainsKey("categoryref")) settings.Add("categoryref", "0");
                var categoryref = settings["categoryref"];

                if (categoryref == "") return false;

                //get data
                var prodData = ProductUtils.GetProductData(productitemid, _lang);

                var l = prodData.GetCategories("",true);

                var cat = from i in l where i.categoryref == categoryref select i;

                if (cat.Any()) return true;

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        private String GetProductCategories(HttpContext context)
        {
            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                var productitemid = settings["itemid"];

                // get template
                var themeFolder = StoreSettings.Current.ThemeFolder;
                if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                var bodyTempl = templCtrl.GetTemplateData("productadmincategories.html", _lang, true, true, true, StoreSettings.Current.Settings());

                //get data
                var prodData = ProductUtils.GetProductData(productitemid, _lang);
                var strOut = GenXmlFunctions.RenderRepeater(prodData.GetCategories("cat"), bodyTempl);                
                
                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String GetProductGroupCategories(HttpContext context)
        {
            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                var productitemid = settings["itemid"];

                // get template
                var themeFolder = StoreSettings.Current.ThemeFolder;
                if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                var bodyTempl = templCtrl.GetTemplateData("productadmingroupcategories.html", _lang, true, true, true, StoreSettings.Current.Settings());

                //get data
                var prodData = ProductUtils.GetProductData(productitemid, _lang);
                var strOut = GenXmlFunctions.RenderRepeater(prodData.GetCategories("!cat"), bodyTempl);

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String GetProductRelated(HttpContext context)
        {
            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                var productitemid = settings["itemid"];

                // get template
                var themeFolder = StoreSettings.Current.ThemeFolder;
                if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                var bodyTempl = templCtrl.GetTemplateData("productadminrelated.html", _lang, true, true, true, StoreSettings.Current.Settings());

                //get data
                var prodData = ProductUtils.GetProductData(productitemid, _lang);
                var strOut = GenXmlFunctions.RenderRepeater(prodData.GetRelatedProducts(), bodyTempl);

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String GetProductClients(HttpContext context)
        {
            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                var productitemid = settings["itemid"];

                // get template
                var themeFolder = StoreSettings.Current.ThemeFolder;
                if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                var bodyTempl = templCtrl.GetTemplateData("productadminclient.html", _lang, true, true, true, StoreSettings.Current.Settings());

                //get data
                var prodData = ProductUtils.GetProductData(productitemid, _lang);
                //var strOut = GenXmlFunctions.RenderRepeater(prodData.GetClients(), bodyTempl);

                //return strOut;
                return "";

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String GetClientSelectList(HttpContext context)
        {
            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                var productitemid = settings["itemid"];
                if (!settings.ContainsKey("header")) settings.Add("header", "");
                if (!settings.ContainsKey("body")) settings.Add("body", "");
                if (!settings.ContainsKey("footer")) settings.Add("footer", "");
                if (!settings.ContainsKey("searchtext")) settings.Add("searchtext", "");

                // get template
                var themeFolder = StoreSettings.Current.ThemeFolder;
                if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                var headTempl = templCtrl.GetTemplateData(settings["header"], _lang, true, true, true, StoreSettings.Current.Settings());
                var bodyTempl = templCtrl.GetTemplateData(settings["body"], _lang, true, true, true, StoreSettings.Current.Settings());
                var footTempl = templCtrl.GetTemplateData(settings["footer"], _lang, true, true, true, StoreSettings.Current.Settings());

                //get data
                var prodData = ProductUtils.GetProductData(productitemid, _lang);
                var objCtrl = new NBrightBuyController();
                var userlist = objCtrl.GetDnnUsers(prodData.Info.PortalId, "%" + settings["searchtext"] + "%", 0,1,20,20);

                var l = new List<NBrightInfo>();
                l.Add(prodData.Info);
                var strHead = GenXmlFunctions.RenderRepeater(l, headTempl);
                var strOut = GenXmlFunctions.RenderRepeater(userlist, bodyTempl);
                var strFoot = GenXmlFunctions.RenderRepeater(l, footTempl);

                return strHead + strOut + strFoot;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String AddProductModels(HttpContext context)
        {

            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                if (!settings.ContainsKey("addqty")) settings.Add("addqty", "1");
                var productitemid = settings["itemid"];
                var qty = settings["addqty"];
                if (!Utils.IsNumeric(qty)) qty = "1";

                var strOut = "No Product ID ('itemid' hidden fields needed on input form)";
                if (Utils.IsNumeric(productitemid))
                {
                    var itemId = Convert.ToInt32(productitemid);
                    var prodData = ProductUtils.GetProductData(itemId, _lang);
                    var lp = 1;
                    var rtnKeys = new List<String>();
                    while (lp <= Convert.ToInt32(qty))
                    {
                        rtnKeys.Add(prodData.AddNewModel());
                        lp += 1;
                        if (lp > 50) break;  // we don;t want to create a stupid amount, it will slow the system!!!
                    }
                    prodData.Save();
                    ProductUtils.RemoveProductDataCache(itemId, StoreSettings.Current.EditLanguage);
                    prodData = ProductUtils.GetProductData(productitemid, _lang);
                    var rtnList = new List<NBrightInfo>();
                    foreach (var k in rtnKeys)
                    {
                        rtnList.Add(prodData.GetModel(k));
                    }

                    // get template
                    var themeFolder = StoreSettings.Current.ThemeFolder;
                    if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                    var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                    var bodyTempl = templCtrl.GetTemplateData("productadminmodels.html", _lang, true, true, true, StoreSettings.Current.Settings());

                    NBrightBuyUtils.RemoveModCachePortalWide(prodData.Info.PortalId);

                    //get data
                    strOut = GenXmlFunctions.RenderRepeater(rtnList, bodyTempl);
                }

                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private String AddProductOptions(HttpContext context)
        {

            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                if (!settings.ContainsKey("addqty")) settings.Add("addqty", "1");
                var productitemid = settings["itemid"];
                var qty = settings["addqty"];
                if (!Utils.IsNumeric(qty)) qty = "1";

                var strOut = "No Product ID ('itemid' hidden fields needed on input form)";
                if (Utils.IsNumeric(productitemid))
                {
                    var itemId = Convert.ToInt32(productitemid);
                    var prodData = ProductUtils.GetProductData(itemId, _lang);
                    var lp = 1;
                    var rtnKeys = new List<String>();
                    while (lp <= Convert.ToInt32(qty))
                    {
                        rtnKeys.Add(prodData.AddNewOption());
                        lp += 1;
                        if (lp > 50) break;  // we don;t want to create a stupid amount, it will slow the system!!!
                    }
                    prodData.Save();
                    ProductUtils.RemoveProductDataCache(itemId, StoreSettings.Current.EditLanguage);
                    prodData = ProductUtils.GetProductData(productitemid, _lang);
                    var rtnList = new List<NBrightInfo>();
                    foreach (var k in rtnKeys)
                    {
                        rtnList.Add(prodData.GetOption(k));
                    }

                    // get template
                    var themeFolder = StoreSettings.Current.ThemeFolder;
                    if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                    var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                    var bodyTempl = templCtrl.GetTemplateData("productadminoptions.html", _lang, true, true, true, StoreSettings.Current.Settings());

                    NBrightBuyUtils.RemoveModCachePortalWide(prodData.Info.PortalId);

                    //get data
                    strOut = GenXmlFunctions.RenderRepeater(rtnList, bodyTempl);
                }

                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private String AddProductOptionValues(HttpContext context)
        {

            try
            {
                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("itemid")) settings.Add("itemid", "");
                if (!settings.ContainsKey("addqty")) settings.Add("addqty", "1");
                if (!settings.ContainsKey("selectedoptionid")) return "";

                var optionid = settings["selectedoptionid"];
                var productitemid = settings["itemid"];
                var qty = settings["addqty"];
                if (!Utils.IsNumeric(qty)) qty = "1";

                var strOut = "No Product ID ('itemid' hidden fields needed on input form)";
                if (Utils.IsNumeric(productitemid))
                {
                    var itemId = Convert.ToInt32(productitemid);
                    var prodData = ProductUtils.GetProductData(itemId, _lang);
                    var lp = 1;
                    var rtnKeys = new List<String>();
                    while (lp <= Convert.ToInt32(qty))
                    {
                        rtnKeys.Add(prodData.AddNewOptionValue(optionid));
                        lp += 1;
                        if (lp > 50) break;  // we don;t want to create a stupid amount, it will slow the system!!!
                    }
                    prodData.Save();
                    ProductUtils.RemoveProductDataCache(itemId, StoreSettings.Current.EditLanguage);
                    prodData = ProductUtils.GetProductData(productitemid, _lang);

                    var rtnList = new List<NBrightInfo>();
                    foreach (var k in rtnKeys)
                    {
                        rtnList.Add(prodData.GetOptionValue(optionid, k));
                    }
                    
                    // get template
                    var themeFolder = StoreSettings.Current.ThemeFolder;
                    if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                    var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                    var bodyTempl = templCtrl.GetTemplateData("productadminoptionvalues.html", _lang, true, true, true, StoreSettings.Current.Settings());

                    NBrightBuyUtils.RemoveModCachePortalWide(prodData.Info.PortalId);

                    //get data
                    strOut = GenXmlFunctions.RenderRepeater(rtnList, bodyTempl);
                }

                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private string AddProductCategory(HttpContext context)
        {
            try
            {
                var settings = GetAjaxFields(context);
                var parentitemid = "";
                var xrefitemid = "";
                if (settings.ContainsKey("itemid")) parentitemid = settings["itemid"];
                if (settings.ContainsKey("selectedcatid")) xrefitemid = settings["selectedcatid"];
                if (Utils.IsNumeric(xrefitemid) && Utils.IsNumeric(parentitemid))
                {
                    var prodData = ProductUtils.GetProductData(Convert.ToInt32(parentitemid), _lang, false);
                    prodData.AddCategory(Convert.ToInt32(xrefitemid));
                    NBrightBuyUtils.RemoveModCachePortalWide(prodData.Info.PortalId);
                    return GetProductCategories(context);
                }
                return "Invalid parentitemid or xrefitmeid";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private string AddProductGroupCategory(HttpContext context)
        {
            try
            {
                var settings = GetAjaxFields(context);
                var parentitemid = "";
                var xrefitemid = "";
                if (settings.ContainsKey("itemid")) parentitemid = settings["itemid"];
                if (settings.ContainsKey("selectedcatid")) xrefitemid = settings["selectedcatid"];
                if (Utils.IsNumeric(xrefitemid) && Utils.IsNumeric(parentitemid))
                {
                    var prodData = ProductUtils.GetProductData(Convert.ToInt32(parentitemid), _lang, false);
                    prodData.AddCategory(Convert.ToInt32(xrefitemid));
                    NBrightBuyUtils.RemoveModCachePortalWide(prodData.Info.PortalId);
                    return GetProductGroupCategories(context);
                }
                return "Invalid parentitemid or xrefitmeid";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private string SetDefaultCategory(HttpContext context)
        {
            try
            {
                var settings = GetAjaxFields(context);
                var parentitemid = "";
                var xrefitemid = "";
                if (settings.ContainsKey("itemid")) parentitemid = settings["itemid"];
                if (settings.ContainsKey("selectedcatid")) xrefitemid = settings["selectedcatid"];
                if (Utils.IsNumeric(xrefitemid) && Utils.IsNumeric(parentitemid))
                {
                    var prodData = ProductUtils.GetProductData(Convert.ToInt32(parentitemid), _lang, false);
                    prodData.SetDefaultCategory(Convert.ToInt32(xrefitemid));
                    NBrightBuyUtils.RemoveModCachePortalWide(prodData.Info.PortalId);
                    return GetProductCategories(context);
                }
                return "Invalid parentitemid or xrefitmeid";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private string RemoveProductCategory(HttpContext context)
        {
            try
            {
                var settings = GetAjaxFields(context);
                var parentitemid = "";
                var xrefitemid = "";
                if (settings.ContainsKey("itemid")) parentitemid = settings["itemid"];
                if (settings.ContainsKey("selectedcatid")) xrefitemid = settings["selectedcatid"];
                if (Utils.IsNumeric(xrefitemid) && Utils.IsNumeric(parentitemid))
                {
                    var prodData = ProductUtils.GetProductData(Convert.ToInt32(parentitemid), _lang, false);
                    prodData.RemoveCategory(Convert.ToInt32(xrefitemid));
                    NBrightBuyUtils.RemoveModCachePortalWide(prodData.Info.PortalId);
                    return GetProductCategories(context);
                }
                return "Invalid parentitemid or xrefitmeid";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private string RemoveProductGroupCategory(HttpContext context)
        {
            try
            {
                var settings = GetAjaxFields(context);
                var parentitemid = "";
                var xrefitemid = "";
                if (settings.ContainsKey("itemid")) parentitemid = settings["itemid"];
                if (settings.ContainsKey("selectedcatid")) xrefitemid = settings["selectedcatid"];
                if (Utils.IsNumeric(xrefitemid) && Utils.IsNumeric(parentitemid))
                {
                    var prodData = ProductUtils.GetProductData(Convert.ToInt32(parentitemid), _lang, false);
                    prodData.RemoveCategory(Convert.ToInt32(xrefitemid));
                    NBrightBuyUtils.RemoveModCachePortalWide(prodData.Info.PortalId);
                    return GetProductGroupCategories(context);
                }
                return "Invalid parentitemid or xrefitmeid";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private string RemoveRelatedProduct(HttpContext context)
        {
            try
            {
                var settings = GetAjaxFields(context);
                var productid = "";
                var selectedrelatedid = "";
                if (settings.ContainsKey("itemid")) productid = settings["itemid"];
                if (settings.ContainsKey("selectedrelatedid")) selectedrelatedid = settings["selectedrelatedid"];
                if (Utils.IsNumeric(productid) && Utils.IsNumeric(selectedrelatedid))
                {
                    var prodData = ProductUtils.GetProductData(Convert.ToInt32(productid), _lang, false);
                    prodData.RemoveRelatedProduct(Convert.ToInt32(selectedrelatedid));
                    NBrightBuyUtils.RemoveModCachePortalWide(prodData.Info.PortalId);
                    return GetProductRelated(context);
                }
                return "Invalid itemid or selectedrelatedid";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private string AddRelatedProduct(HttpContext context)
        {
            try
            {
                var settings = GetAjaxFields(context);
                var productid = "";
                var selectedrelatedid = "";
                if (settings.ContainsKey("itemid")) productid = settings["itemid"];
                if (settings.ContainsKey("selectedrelatedid")) selectedrelatedid = settings["selectedrelatedid"];
                if (Utils.IsNumeric(selectedrelatedid) && Utils.IsNumeric(productid))
                {
                    var prodData = ProductUtils.GetProductData(Convert.ToInt32(productid), _lang, false);
                    if (prodData.Exists) prodData.AddRelatedProduct(Convert.ToInt32(selectedrelatedid));

                    // do bi-direction
                    var prodData2 = ProductUtils.GetProductData(Convert.ToInt32(selectedrelatedid), _lang, false);
                    if (prodData2.Exists) prodData2.AddRelatedProduct(Convert.ToInt32(productid));

                    NBrightBuyUtils.RemoveModCachePortalWide(prodData.Info.PortalId);
                    return GetProductRelated(context);
                }
                return "Invalid itemid or selectedrelatedid";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        private string RemoveProductClient(HttpContext context)
        {
            try
            {
                var settings = GetAjaxFields(context);
                var productid = "";
                var selecteduserid = "";
                if (settings.ContainsKey("itemid")) productid = settings["itemid"];
                if (settings.ContainsKey("selecteduserid")) selecteduserid = settings["selecteduserid"];
                if (Utils.IsNumeric(productid) && Utils.IsNumeric(selecteduserid))
                {
                    var prodData = ProductUtils.GetProductData(Convert.ToInt32(productid), _lang, false);
                    if (!(NBrightBuyUtils.IsClientOnly() && (Convert.ToInt32(selecteduserid) == UserController.GetCurrentUserInfo().UserID)))
                    {
                        // ClientEditor role cannot remove themselves.
                        //prodData.RemoveClient(Convert.ToInt32(selecteduserid));
                    }
                    NBrightBuyUtils.RemoveModCachePortalWide(prodData.Info.PortalId);
                    return GetProductClients(context);
                }
                return "Invalid itemid or selectedrelatedid";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
       
        private string AddProductClient(HttpContext context)
        {
            try
            {
                var settings = GetAjaxFields(context);
                var productid = "";
                var selecteduserid = "";
                if (settings.ContainsKey("itemid")) productid = settings["itemid"];
                if (settings.ContainsKey("selecteduserid")) selecteduserid = settings["selecteduserid"];
                if (Utils.IsNumeric(selecteduserid) && Utils.IsNumeric(productid))
                {
                    var prodData = ProductUtils.GetProductData(Convert.ToInt32(productid), _lang, false);
                    //if (prodData.Exists) prodData.AddClient(Convert.ToInt32(selecteduserid));

                    NBrightBuyUtils.RemoveModCachePortalWide(prodData.Info.PortalId);
                    return GetProductClients(context);
                }
                return "Invalid itemid or selecteduserid";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        #endregion

        #region "clients"

        private String GetClientDiscountCodes(HttpContext context)
        {
            try
            {
                //get uploaded params
                var strOut = "";
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("userid")) settings.Add("userid", "");
                var userid = settings["userid"];
                if (!settings.ContainsKey("portalid")) settings.Add("portalid", "");
                var portalid = settings["portalid"];
                if (Utils.IsNumeric(portalid) && Utils.IsNumeric(userid))
                {
                    // get template
                    var themeFolder = StoreSettings.Current.ThemeFolder;
                    if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                    var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                    var bodyTempl = templCtrl.GetTemplateData("clientdiscountcodes.html", _lang, true, true, true, StoreSettings.Current.Settings());
                    bodyTempl = Utils.ReplaceSettingTokens(bodyTempl, StoreSettings.Current.Settings());
                    //get data
                    var clientData = new ClientData(Convert.ToInt32(portalid), Convert.ToInt32(userid));
                    strOut = GenXmlFunctions.RenderRepeater(clientData.DiscountCodes, bodyTempl);                    
                }

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String AddClientDiscountCodes(HttpContext context)
        {

            try
            {
                var strOut = "Missing data ('userid', 'portalid' hidden fields needed on input form)";

                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("addqty")) settings.Add("addqty", "1");
                if (!settings.ContainsKey("userid")) settings.Add("userid", "");
                var userid = settings["userid"];
                if (!settings.ContainsKey("portalid")) settings.Add("portalid", "");
                var portalid = settings["portalid"];
                if (Utils.IsNumeric(portalid) && Utils.IsNumeric(userid))
                {
                    var clientData = new ClientData(Convert.ToInt32(portalid), Convert.ToInt32(userid));

                    var qty = settings["addqty"];
                    if (!Utils.IsNumeric(qty)) qty = "1";

                       var lp = 1;
                       var modelcount = clientData.DiscountCodes.Count;
                        while (lp <= Convert.ToInt32(qty))
                        {
                            clientData.AddNewDiscountCode();
                            lp += 1;
                            if (lp > 10) break; // we don;t want to create a stupid amount, it will slow the system!!!
                        }
                        clientData.Save();
                        var modelcount2 = clientData.DiscountCodes.Count;
                        var rtnList = new List<NBrightInfo>();
                        for (var i = modelcount; i < modelcount2; i++)
                        {
                            rtnList.Add(clientData.DiscountCodes[i]);
                        }

                        // get template
                        var themeFolder = StoreSettings.Current.ThemeFolder;
                        if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                        var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                        var bodyTempl = templCtrl.GetTemplateData("clientdiscountcodes.html", _lang, true, true, true, StoreSettings.Current.Settings());

                        //get data
                        strOut = GenXmlFunctions.RenderRepeater(rtnList, bodyTempl);
                }
                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private String GetClientVoucherCodes(HttpContext context)
        {
            try
            {
                //get uploaded params
                var strOut = "";
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("userid")) settings.Add("userid", "");
                var userid = settings["userid"];
                if (!settings.ContainsKey("portalid")) settings.Add("portalid", "");
                var portalid = settings["portalid"];
                if (Utils.IsNumeric(portalid) && Utils.IsNumeric(userid))
                {
                    // get template
                    var themeFolder = StoreSettings.Current.ThemeFolder;
                    if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                    var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                    var bodyTempl = templCtrl.GetTemplateData("clientvouchercodes.html", _lang, true, true, true, StoreSettings.Current.Settings());
                    bodyTempl = Utils.ReplaceSettingTokens(bodyTempl, StoreSettings.Current.Settings());
                    //get data
                    var clientData = new ClientData(Convert.ToInt32(portalid), Convert.ToInt32(userid));
                    strOut = GenXmlFunctions.RenderRepeater(clientData.VoucherCodes, bodyTempl);
                }

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String AddClientVoucherCodes(HttpContext context)
        {

            try
            {
                var strOut = "Missing data ('userid', 'portalid' hidden fields needed on input form)";

                //get uploaded params
                var settings = GetAjaxFields(context);
                if (!settings.ContainsKey("addqty")) settings.Add("addqty", "1");
                if (!settings.ContainsKey("userid")) settings.Add("userid", "");
                var userid = settings["userid"];
                if (!settings.ContainsKey("portalid")) settings.Add("portalid", "");
                var portalid = settings["portalid"];
                if (Utils.IsNumeric(portalid) && Utils.IsNumeric(userid))
                {
                    var clientData = new ClientData(Convert.ToInt32(portalid), Convert.ToInt32(userid));

                    var qty = settings["addqty"];
                    if (!Utils.IsNumeric(qty)) qty = "1";

                    var lp = 1;
                    var modelcount = clientData.VoucherCodes.Count;
                    while (lp <= Convert.ToInt32(qty))
                    {
                        clientData.AddNewVoucherCode();
                        lp += 1;
                        if (lp > 10) break; // we don;t want to create a stupid amount, it will slow the system!!!
                    }
                    clientData.Save();
                    var modelcount2 = clientData.VoucherCodes.Count;
                    var rtnList = new List<NBrightInfo>();
                    for (var i = modelcount; i < modelcount2; i++)
                    {
                        rtnList.Add(clientData.VoucherCodes[i]);
                    }

                    // get template
                    var themeFolder = StoreSettings.Current.ThemeFolder;
                    if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
                    var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);
                    var bodyTempl = templCtrl.GetTemplateData("clientvouchercodes.html", _lang, true, true, true, StoreSettings.Current.Settings());

                    //get data
                    strOut = GenXmlFunctions.RenderRepeater(rtnList, bodyTempl);
                }
                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }



        #endregion

        #region "Front Office Actions"

        private String RenderCart(HttpContext context)
        {
            var ajaxInfo = GetAjaxInfo(context);
            var carttemplate = ajaxInfo.GetXmlProperty("genxml/hidden/carttemplate");
            var theme = ajaxInfo.GetXmlProperty("genxml/hidden/carttheme");
            var lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang");
            var controlpath = ajaxInfo.GetXmlProperty("genxml/hidden/controlpath");
            if (controlpath == "") controlpath = "/DesktopModules/NBright/NBrightBuy";
            var razorTempl = "";
            if (carttemplate != "")
            {
                if (lang == "") lang = Utils.GetCurrentCulture();
                var currentcart = new CartData(PortalSettings.Current.PortalId);
                razorTempl = NBrightBuyUtils.RazorTemplRender(carttemplate, 0,"", currentcart, controlpath, theme, lang, StoreSettings.Current.Settings());
            }
            return razorTempl;
        }

        /// <summary>
        /// This token used the ajax posted context data to render the razor template specified in "carttemplate"
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private String RenderPostData(HttpContext context)
        {
            var ajaxInfo = GetAjaxInfo(context);
            var carttemplate = ajaxInfo.GetXmlProperty("genxml/hidden/carttemplate");
            var theme = ajaxInfo.GetXmlProperty("genxml/hidden/carttheme");
            var lang = ajaxInfo.GetXmlProperty("genxml/hidden/lang");
            var controlpath = ajaxInfo.GetXmlProperty("genxml/hidden/controlpath");
            if (controlpath == "") controlpath = "/DesktopModules/NBright/NBrightBuy";
            var razorTempl = "";
            if (carttemplate != "")
            {
                if (lang == "") lang = Utils.GetCurrentCulture();
                razorTempl = NBrightBuyUtils.RazorTemplRender(carttemplate, 0, "", ajaxInfo, controlpath, theme, lang, StoreSettings.Current.Settings());
            }
            return razorTempl;
        }

        private string AddToBasket(HttpContext context)
        {
            try
            {
                var strOut = "";
                var ajaxInfo = GetAjaxInfo(context);
                var settings = ajaxInfo.ToDictionary();

                if (settings.ContainsKey("productid"))
                {
                    if (!settings.ContainsKey("portalid")) settings.Add("portalid", PortalSettings.Current.PortalId.ToString("")); // aways make sure we have portalid in settings

                    var currentcart = new CartData(Convert.ToInt16(settings["portalid"]));
                    //currentcart.AddAjaxItem(ajaxInfo, StoreSettings.Current.SettingsInfo,StoreSettings.Current.DebugMode);
                    currentcart.Save(StoreSettings.Current.DebugMode);
                    strOut = "OK";
                }


                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private string AddAllToBasket(HttpContext context)
        {
            try
            {
                var strOut = "";
                var ajaxInfoList = GetAjaxInfoList(context);

                foreach (var ajaxInfo in ajaxInfoList)
                {
                    var settings = ajaxInfo.ToDictionary();

                    if (settings.ContainsKey("productid"))
                    {
                        if (!settings.ContainsKey("portalid")) settings.Add("portalid", PortalSettings.Current.PortalId.ToString("")); // aways make sure we have portalid in settings

                        var currentcart = new CartData(Convert.ToInt16(settings["portalid"]));
                        //currentcart.AddAjaxItem(ajaxInfo, StoreSettings.Current.SettingsInfo, StoreSettings.Current.DebugMode);
                        currentcart.Save(StoreSettings.Current.DebugMode);
                        strOut = "OK";
                    }

                }

                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private string RecalculateCart(HttpContext context)
        {
            try
            {
                var strOut = "";
                var ajaxInfoList = GetAjaxInfoList(context);
                var currentcart = new CartData(PortalSettings.Current.PortalId);
                foreach (var ajaxInfo in ajaxInfoList)
                {
                        currentcart.MergeCartInputData(currentcart.GetItemIndex(ajaxInfo.GetXmlProperty("genxml/hidden/itemcode")), ajaxInfo);
                        currentcart.Save(StoreSettings.Current.DebugMode);
                        strOut = "OK";
                }
                return strOut;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private string UpdateProfile(HttpContext context)
        {
            try
            {
                var ajaxInfo = GetAjaxInfo(context, true);
                var profileData = new ProfileData();
                //profileData.UpdateProfileAjax(ajaxInfo.XMLData, StoreSettings.Current.DebugMode);

                return "OK";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private string UpdateCartAddress(HttpContext context,String addresstype = "")
        {
            try
            {
                var currentcart = new CartData(PortalSettings.Current.PortalId);
                var ajaxInfo = GetAjaxInfo(context,true);

                currentcart.PurchaseInfo.SetXmlProperty("genxml/currentcartstage", "cartsummary"); // (Legacy) we need to set this so the cart calcs shipping

                if (addresstype == "bill")
                {
                    currentcart.AddBillingAddress(ajaxInfo);
                    currentcart.Save();
                }

                if (addresstype == "ship")
                {
                    if (currentcart.GetShippingOption() == "2") // need to test this, becuase in legacy code the shipping option is set to "2" when we save the shipping address.
                    {
                        currentcart.AddShippingAddress(ajaxInfo);
                        currentcart.Save();
                    }
                }

                if (addresstype == "shipoption")
                {
                    var shipoption = ajaxInfo.GetXmlProperty("genxml/radiobuttonlist/rblshippingoptions");
                    currentcart.SetShippingOption(shipoption);
                    currentcart.Save();
                }

                return addresstype;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private string RedirectToPayment(HttpContext context)
        {
            try
            {
                RecalculateSummary(context);

                var currentcart = new CartData(PortalSettings.Current.PortalId);

                if (currentcart.GetCartItemList().Count > 0)
                {
                    currentcart.SetValidated(true);
                    if (currentcart.EditMode == "E") currentcart.ConvertToOrder();
                }
                else
                {
                    currentcart.SetValidated(true);
                }
                currentcart.Save();

                var rtnurl = Globals.NavigateURL(StoreSettings.Current.PaymentTabId);
                if (currentcart.EditMode == "E")
                {
                    // is order being edited, so return to order status after edit.
                    // ONLY if the cartsummry is being displayed to the manager.
                    currentcart.ConvertToOrder();
                    // redirect to back office
                    var param = new string[2];
                    param[0] = "ctrl=orders";
                    param[1] = "eid=" + currentcart.PurchaseInfo.ItemID.ToString("");
                    var strbackofficeTabId = StoreSettings.Current.Get("backofficetabid");
                    var backofficeTabId = PortalSettings.Current.ActiveTab.TabID;
                    if (Utils.IsNumeric(strbackofficeTabId)) backofficeTabId = Convert.ToInt32(strbackofficeTabId);
                    rtnurl = Globals.NavigateURL(backofficeTabId, "", param);
                }
                return rtnurl;

            }
            catch (Exception ex)
            {
                Exceptions.LogException(ex);
                return "ERROR";
            }
        }

        private string RecalculateSummary(HttpContext context)
        {
            try
            {
                var currentcart = new CartData(PortalSettings.Current.PortalId);
                var ajaxInfo = GetAjaxInfo(context, true);
                var shipoption = currentcart.GetShippingOption(); // ship option already set in address update.

                currentcart.AddExtraInfo(ajaxInfo);
                currentcart.SetShippingOption(shipoption);
                currentcart.PurchaseInfo.SetXmlProperty("genxml/currentcartstage", "cartsummary"); // (Legacy) we need to set this so the cart calcs shipping
                currentcart.PurchaseInfo.SetXmlProperty("genxml/extrainfo/genxml/radiobuttonlist/shippingprovider", ajaxInfo.GetXmlProperty("genxml/radiobuttonlist/shippingprovider"));

                var shippingproductcode = ajaxInfo.GetXmlProperty("genxml/hidden/shippingproductcode");
                currentcart.PurchaseInfo.SetXmlProperty("genxml/shippingproductcode", shippingproductcode);
                var pickuppointref = ajaxInfo.GetXmlProperty("genxml/hidden/pickuppointref");
                currentcart.PurchaseInfo.SetXmlProperty("genxml/pickuppointref", pickuppointref);
                var pickuppointaddr = ajaxInfo.GetXmlProperty("genxml/hidden/pickuppointaddr");
                currentcart.PurchaseInfo.SetXmlProperty("genxml/pickuppointaddr", pickuppointaddr);

                currentcart.Lang = ajaxInfo.Lang;  // set lang so we can send emails in same language the order was made in.

                currentcart.Save();

                return "OK";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private string RemoveFromCart(HttpContext context)
        {
            try
            {
                var ajaxInfo = GetAjaxInfo(context);
                var currentcart = new CartData(PortalSettings.Current.PortalId);
                currentcart.RemoveItem(ajaxInfo.GetXmlProperty("genxml/hidden/itemcode"));
                currentcart.Save(StoreSettings.Current.DebugMode);
                return "OK";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }


        private String GetShippingProviderTemplates(HttpContext context)
        {
            var ajaxInfo = GetAjaxInfo(context);
            var activeprovider = ajaxInfo.GetXmlProperty("genxml/hidden/shippingprovider");
            var currentcart = new CartData(PortalSettings.Current.PortalId);

            var shipoption = currentcart.GetShippingOption(); // we don't want to overwrite the selected shipping option.
            currentcart.AddExtraInfo(ajaxInfo);
            currentcart.SetShippingOption(shipoption);
            currentcart.PurchaseInfo.SetXmlProperty("genxml/currentcartstage", "cartsummary"); // (Legacy) we need to set this so the cart calcs shipping
            currentcart.Save();

            if (activeprovider == "") activeprovider = currentcart.PurchaseInfo.GetXmlProperty("genxml/extrainfo/genxml/radiobuttonlist/shippingprovider");


            var strRtn = "";
            var pluginData = new PluginData(PortalSettings.Current.PortalId);
            var provList = pluginData.GetShippingProviders();
            if (provList != null && provList.Count > 0)
            {
                if (activeprovider == "") activeprovider = provList.First().Key;
                foreach (var d in provList)
                {
                    var p = d.Value;
                    var shippingkey = p.GetXmlProperty("genxml/textbox/ctrl");
                    var shipprov = ShippingInterface.Instance(shippingkey);
                    if (shipprov != null)
                    {
                        if (activeprovider == d.Key)
                        {
                            var razorTempl = shipprov.GetTemplate(currentcart.PurchaseInfo);
                            var objList = new List<NBrightInfo>();
                            objList.Add(currentcart.PurchaseInfo);
                            if (razorTempl.StartsWith("@"))
                            {
                                strRtn += NBrightBuyUtils.RazorRender(objList, razorTempl, shippingkey + "shippingtemplate", StoreSettings.Current.DebugMode);
                            }
                            else
                            {
                                strRtn += GenXmlFunctions.RenderRepeater(objList[0], razorTempl, "", "XMLData", "", StoreSettings.Current.Settings(), null);
                            }
                        }
                    }
                }
            }
            return strRtn;
        }


        #endregion


        #region "Settings"

        private String GetSettings(HttpContext context, bool clearCache = false)
        {
            try
            {
                var strOut = "";
                //get uploaded params
                var ajaxInfo = GetAjaxInfo(context);

                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                var razortemplate = ajaxInfo.GetXmlProperty("genxml/hidden/razortemplate");
                var themefolder = ajaxInfo.GetXmlProperty("genxml/dropdownlist/themefolder");
                var controlpath = ajaxInfo.GetXmlProperty("genxml/hidden/controlpath");
                if (controlpath == "") controlpath = "/DesktopModules/NBright/NBrightBuy";
                if (razortemplate == "") razortemplate = "settings.cshtml";
                if (moduleid == "") moduleid = "-1";

                // do edit field data if a itemid has been selected
                var obj = NBrightBuyUtils.GetSettings(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid));
                obj.ModuleId = Convert.ToInt32(moduleid); // assign for new records
                strOut = NBrightBuyUtils.RazorTemplRender(razortemplate, obj.ModuleId, "settings", obj, controlpath, themefolder, _lang,null);

                return strOut;

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private String SaveSettings(HttpContext context)
        {
            try
            {
                var objCtrl = new NBrightBuyController();

                //get uploaded params
                var ajaxInfo = GetAjaxInfo(context);

                var moduleid = ajaxInfo.GetXmlProperty("genxml/hidden/moduleid");
                if (Utils.IsNumeric(moduleid))
                {
                    // get DB record
                    var nbi = NBrightBuyUtils.GetSettings(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid)); 
                    if (nbi.ModuleId == 0) // new setting record
                    {
                        nbi = CreateSettingsInfo(moduleid, nbi);
                    }
                    // get data passed back by ajax
                    var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
                    // update record with ajax data
                    nbi.UpdateAjax(strIn);


                    if (nbi.GetXmlProperty("genxml/hidden/modref") == "") nbi.SetXmlProperty("genxml/hidden/modref", Utils.GetUniqueKey(10));
                    if (nbi.TextData == "") nbi.TextData = "NBrightBuy";
                    objCtrl.Update(nbi);
                    NBrightBuyUtils.RemoveModCachePortalWide(PortalSettings.Current.PortalId); // make sure all new settings are used.
                }
                return "";

            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        private NBrightInfo CreateSettingsInfo(String moduleid, NBrightInfo nbi)
        {

            var objCtrl = new NBrightBuyController();
            nbi = objCtrl.GetByType(PortalSettings.Current.PortalId, Convert.ToInt32(moduleid), "SETTINGS");
            if (nbi == null)
            {
                nbi = new NBrightInfo(true); // populate empty XML so we can update nodes.
                nbi.GUIDKey = "";
                nbi.PortalId = PortalSettings.Current.PortalId;
                nbi.ModuleId = Convert.ToInt32(moduleid);
                nbi.TypeCode = "SETTINGS";
                nbi.Lang = "";
            }
            //rebuild xml
            nbi.ModuleId = Convert.ToInt32(moduleid);
            nbi.GUIDKey = Utils.GetUniqueKey(10);
            nbi.SetXmlProperty("genxml/hidden/modref", nbi.GUIDKey);
            return nbi;
        }


        #endregion

        #region "functions"

        private String GetProductListData(Dictionary<String, String> settings,bool paging = true)
        {
            var strOut = "";

            if (!settings.ContainsKey("header")) settings.Add("header", "");
            if (!settings.ContainsKey("body")) settings.Add("body", "");
            if (!settings.ContainsKey("footer")) settings.Add("footer", "");
            if (!settings.ContainsKey("filter")) settings.Add("filter", "");
            if (!settings.ContainsKey("orderby")) settings.Add("orderby", "");
            if (!settings.ContainsKey("returnlimit")) settings.Add("returnlimit", "0");
            if (!settings.ContainsKey("pagenumber")) settings.Add("pagenumber", "0");
            if (!settings.ContainsKey("pagesize")) settings.Add("pagesize", "0");
            if (!settings.ContainsKey("searchtext")) settings.Add("searchtext", "");
            if (!settings.ContainsKey("searchcategory")) settings.Add("searchcategory", "");
            if (!settings.ContainsKey("cascade")) settings.Add("cascade", "False");

            var header = settings["header"];
            var body = settings["body"];
            var footer = settings["footer"];
            var filter = settings["filter"];
            var orderby = settings["orderby"];
            var returnLimit = Convert.ToInt32(settings["returnlimit"]);    
            var pageNumber = Convert.ToInt32(settings["pagenumber"]);            
            var pageSize = Convert.ToInt32(settings["pagesize"]);
            var cascade = Convert.ToBoolean(settings["cascade"]);

            var searchText = settings["searchtext"];
            var searchCategory = settings["searchcategory"];

            if (searchText != "") filter += " and (NB3.[ProductName] like '%" + searchText + "%' or NB3.[ProductRef] like '%" + searchText + "%' or NB3.[Summary] like '%" + searchText + "%' ) ";

            if (Utils.IsNumeric(searchCategory))
            {
                if (orderby == "{bycategoryproduct}") orderby += searchCategory;
                var objQual = DataProvider.Instance().ObjectQualifier;
                var dbOwner = DataProvider.Instance().DatabaseOwner;
                if (!cascade)
                    filter += " and NB1.[ItemId] in (select parentitemid from " + dbOwner + "[" + objQual + "NBrightBuy] where typecode = 'CATXREF' and XrefItemId = " + searchCategory + ") ";
                else
                    filter += " and NB1.[ItemId] in (select parentitemid from " + dbOwner + "[" + objQual + "NBrightBuy] where (typecode = 'CATXREF' and XrefItemId = " + searchCategory + ") or (typecode = 'CATCASCADE' and XrefItemId = " + searchCategory + ")) ";
            }
            else
            {
                if (orderby == "{bycategoryproduct}") orderby = " order by NB3.productname ";                
            }

            // logic for client list of products
            if (NBrightBuyUtils.IsClientOnly())
            {
                filter += " and NB1.ItemId in (select ParentItemId from dbo.[NBrightBuy] as NBclient where NBclient.TypeCode = 'USERPRDXREF' and NBclient.UserId = " + UserController.GetCurrentUserInfo().UserID.ToString("") + ") ";
            }


            var recordCount = 0;

            var themeFolder = StoreSettings.Current.ThemeFolder;
            if (settings.ContainsKey("themefolder")) themeFolder = settings["themefolder"];
            var templCtrl = NBrightBuyUtils.GetTemplateGetter(themeFolder);

            if (!settings.ContainsKey("portalid")) settings.Add("portalid", PortalSettings.Current.PortalId.ToString("")); // aways make sure we have portalid in settings

            var objCtrl = new NBrightBuyController();

            var headerTempl = templCtrl.GetTemplateData(header, _lang, true, true, true, StoreSettings.Current.Settings());
            var bodyTempl = templCtrl.GetTemplateData(body, _lang, true, true, true, StoreSettings.Current.Settings());
            var footerTempl = templCtrl.GetTemplateData(footer, _lang, true, true, true, StoreSettings.Current.Settings());

            // replace any settings tokens (This is used to place the form data into the SQL)
            headerTempl = Utils.ReplaceSettingTokens(headerTempl, settings);
            headerTempl = Utils.ReplaceUrlTokens(headerTempl);
            bodyTempl = Utils.ReplaceSettingTokens(bodyTempl, settings);
            bodyTempl = Utils.ReplaceUrlTokens(bodyTempl);
            footerTempl = Utils.ReplaceSettingTokens(footerTempl, settings);
            footerTempl = Utils.ReplaceUrlTokens(footerTempl);

            var obj = new NBrightInfo(true);
            strOut = GenXmlFunctions.RenderRepeater(obj, headerTempl);

            if (paging) // get record count for paging
            {
                if (pageNumber == 0) pageNumber = 1;
                if (pageSize == 0) pageSize = StoreSettings.Current.GetInt("pagesize");
                recordCount = objCtrl.GetListCount(PortalSettings.Current.PortalId, -1, "PRD", filter,"PRDLANG",_lang);
                var recordCountAMY = objCtrl.GetListCount(PortalSettings.Current.PortalId, -1, "AMY", filter, "AMYLANG", _lang);
                recordCount += recordCountAMY;
            }

            var objList = objCtrl.GetDataList(PortalSettings.Current.PortalId, -1, "PRD", "PRDLANG", _lang, filter, orderby, StoreSettings.Current.DebugMode,"",returnLimit,pageNumber,pageSize,recordCount);

            // add AMY type entries
            var objListAMY = objCtrl.GetDataList(PortalSettings.Current.PortalId, -1, "AMY", "AMYLANG", _lang, filter, orderby, StoreSettings.Current.DebugMode, "", returnLimit, pageNumber, pageSize, recordCount);
            objList.AddRange(objListAMY);

            strOut += GenXmlFunctions.RenderRepeater(objList, bodyTempl);

            strOut += GenXmlFunctions.RenderRepeater(obj, footerTempl);

            // add paging if needed
            if (paging)
            {
                var pg = new NBrightCore.controls.PagingCtrl();
                strOut += pg.RenderPager(recordCount, pageSize, pageNumber);
            }

            return strOut;
        }

        private Dictionary<String, String> GetAjaxFields(HttpContext context)
        {
            var objInfo = GetAjaxInfo(context);
            var dic =  objInfo.ToDictionary();
            return dic;
        }

        /// <summary>
        /// Put Ajax data into a NBrightInfo class for processing
        /// </summary>
        /// <param name="context">Http context</param>
        /// <param name="updatefields">If true only fields marked with update attribute are returned.</param>
        /// <returns></returns>
        private NBrightInfo GetAjaxInfo(HttpContext context, Boolean updatefields = false)
        {
            var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));

            var objInfo = new NBrightInfo();

            objInfo.ItemID = -1;
            objInfo.TypeCode = "AJAXDATA";
            objInfo.PortalId = PortalSettings.Current.PortalId;
            if (updatefields)
            {
                objInfo.UpdateAjax(strIn);
            }
            else
            {
                var xmlData = GenXmlFunctions.GetGenXmlByAjax(strIn, "");
                objInfo.XMLData = xmlData;
            }
            var dic = objInfo.ToDictionary();
            // set langauge if we have it passed.
            if (dic.ContainsKey("lang") && dic["lang"] != "") _lang = dic["lang"];

            // set the context  culturecode, so any DNN functions use the correct culture (entryurl tag token)
            if (_lang != "" && _lang != System.Threading.Thread.CurrentThread.CurrentCulture.ToString()) System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(_lang);
            objInfo.Lang = _lang; // make sure we have the langauge in the object.

            return objInfo;
        }

        private List<NBrightInfo> GetAjaxInfoList(HttpContext context)
        {
            var rtnList = new List<NBrightInfo>();
            var strIn = HttpUtility.UrlDecode(Utils.RequestParam(context, "inputxml"));
            var xmlDoc1 = new XmlDocument();
            if (!String.IsNullOrEmpty(strIn))
            {

                xmlDoc1.LoadXml(strIn);

                var xmlNodeList = xmlDoc1.SelectNodes("root/*");
                if (xmlNodeList != null)
                {
                    foreach (XmlNode nod in xmlNodeList)
                    {
                        var xmlData = GenXmlFunctions.GetGenXmlByAjax(nod.OuterXml, "");
                        var objInfo = new NBrightInfo();

                        objInfo.ItemID = -1;
                        objInfo.TypeCode = "AJAXDATA";
                        objInfo.PortalId = PortalSettings.Current.PortalId;
            objInfo.XMLData = xmlData;
                        var dic = objInfo.ToDictionary();
            // set langauge if we have it passed.
            if (dic.ContainsKey("lang") && dic["lang"] != "") _lang = dic["lang"];
                        rtnList.Add(objInfo);
                    }
                }
            }
            return rtnList;
        }




        #endregion
    }
}