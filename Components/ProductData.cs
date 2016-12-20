﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Localization;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;

namespace Nevoweb.DNN.NBrightBuy.Components
{
    public class ProductData
    {
        public NBrightInfo Info;
        public NBrightInfo DataRecord;
        public NBrightInfo DataLangRecord;

        public List<NBrightInfo> Models;
        public List<NBrightInfo> Options;
        public List<NBrightInfo> OptionValues;
        public List<NBrightInfo> Imgs;
        public List<NBrightInfo> Docs;

        private String _lang = ""; // needed for webservice
        private int _portalId = -1; // so we don;t need to use Portalsettings.Current (If called from scheduler)
        private StoreSettings _storeSettings = null;

        private String _typeCode = "";
        private String _typeLangCode = "";

        public void ResetData(int productId, String lang, Boolean hydrateLists)
        {
            ResetData(productId, lang, hydrateLists, "PRD");
        }

        // used to reset the data, so we don;t have to create new object in import loops for memory management.
        public void ResetData(int productId, String lang, Boolean hydrateLists, String typeCode)
        {
            _lang = lang;
            _typeCode = typeCode;
            _typeLangCode = typeCode + "LANG";
            #region "Init data objects to prevent possible errors"

            Models = new List<NBrightInfo>();
            Options = new List<NBrightInfo>();
            OptionValues = new List<NBrightInfo>();
            Imgs = new List<NBrightInfo>();
            Docs = new List<NBrightInfo>();

            DataRecord = new NBrightInfo(true);
            DataRecord.TypeCode = _typeCode;
            DataRecord.Lang = "";
            DataLangRecord = new NBrightInfo(true);
            DataLangRecord.TypeCode = _typeLangCode;
            DataLangRecord.Lang = lang;

            #endregion

            LoadData(productId, hydrateLists);

            if (_lang == "")
            {
                _lang = _storeSettings.EditLanguage;
                DataLangRecord.Lang = _lang;
            }
            
        }

        public ProductData()
        {
            // do nothing assume, resetdata will be used later in calling code.
        }


        /// <summary>
        /// Populate the ProductData in this class
        /// </summary>
        /// <param name="productId">productid</param>
        /// <param name="lang">langauge to populate</param>
        /// <param name="hydrateLists">populate the sub data into lists</param>
        /// <param name="typeCode">Typecode of record default "PRD"</param>
        /// <param name="typeLangCode">Langauge Typecode of record default "PRDLANG"</param>
        public ProductData(int productId, String lang, Boolean hydrateLists = true, String typeCode = "PRD")
        {
            ResetData(productId, lang, hydrateLists, typeCode);
        }

        // overload to support out of http context instantiations ie. when the scheduler runs NBrightDnnIdx
        public ProductData(int productId, int portalId, String lang, Boolean hydrateLists = true, String typeCode = "PRD")
        {
            _portalId = portalId;
            ResetData(productId, lang, hydrateLists, typeCode);
        }
        
        #region "public functions/interface"

        /// <summary>
        /// Set to true if product exists
        /// </summary>
        public bool Exists { get; private set; }
        public bool IsInStock { get; private set; }
        public bool IsOnSale { get; private set; }
        public bool DealerIsOnSale { get; private set; }
        public bool ClientFileUpload { get; private set; }


        public Boolean Disabled
        {
            get
            {
                return Info.GetXmlPropertyBool("genxml/checkbox/chkdisable");
            }
        }

        public String SEOName
        {
            get
            {
                var seoname = Info.GetXmlProperty("genxml/lang/genxml/textbox/txtseoname");
                if (seoname == "") seoname = Info.GetXmlProperty("genxml/lang/genxml/textbox/txtproductname");
                return seoname;
            }
        }

        public String ProductRef
        {
            get { return Info.GetXmlProperty("genxml/textbox/txtproductref"); }
        }

        public String ProductName
        {
            get { return Info.GetXmlProperty("genxml/lang/genxml/textbox/txtproductname"); }
        }

        public String SEOTitle
        {
            get
            {
                var strOut = Info.GetXmlProperty("genxml/lang/genxml/textbox/txtseopagetitle");
                if (strOut == "") strOut = Info.GetXmlProperty("genxml/lang/genxml/textbox/txtseoname");
                if (strOut == "") strOut = Info.GetXmlProperty("genxml/lang/genxml/textbox/txtproductname");
                return strOut;
            }
        }

        public String SEOTagwords
        {
            get
            {
                var strOut = Info.GetXmlProperty("genxml/lang/genxml/textbox/txttagwords");
                if (strOut == "") strOut = Info.GetXmlProperty("genxml/lang/genxml/textbox/txtproductname");
                return strOut;
            }
        }

        public String SEODescription
        {
            get
            {
                var strOut = Info.GetXmlProperty("genxml/lang/genxml/textbox/txtsummary");
                if (strOut == "") strOut = Info.GetXmlProperty("genxml/lang/genxml/textbox/txtproductname");
                return strOut;
            }
        }

        public List<NBrightInfo> GetOptionValuesById(String optionid)
        {
            var l = new List<NBrightInfo>();
            if (Info != null)
            {
                var xmlNodList = Info.XMLDoc.SelectNodes("genxml/optionvalues[@optionid='" + optionid + "']/*");
                if (xmlNodList != null && xmlNodList.Count > 0)
                {
                    var lp = 1;
                    foreach (XmlNode xNod in xmlNodList)
                    {
                        var obj = new NBrightInfo();
                        obj.XMLData = xNod.OuterXml;
                        var nodLang = "<genxml>" + Info.GetXmlNode("genxml/lang/genxml/optionvalues[@optionid='" + optionid + "']/genxml[" + lp + "]") + "</genxml>";
                        if (nodLang != "")
                        {
                            obj.SetXmlProperty("genxml/hidden/productid", Info.ItemID.ToString(""));
                            obj.SetXmlProperty("genxml/hidden/lang", Info.Lang.Trim());
                            obj.SetXmlProperty("genxml/hidden/optionid", optionid);
                            var selectSingleNode = xNod.SelectSingleNode("hidden/optionvalueid");
                            if (selectSingleNode != null) obj.SetXmlProperty("genxml/hidden/optionvalueid", selectSingleNode.InnerText);                                
                            obj.AddSingleNode("lang", "", "genxml");
                            obj.AddXmlNode(nodLang, "genxml", "genxml/lang");
                        }
                        obj.ParentItemId = Info.ItemID;
                        l.Add(obj);
                        lp += 1;
                    }
                }
            }
            return l;
        }

        public NBrightInfo GetModel(String modelid)
        {
            if (Models.Count > 0)
            {
                var obj = Models.Where(i => i.GetXmlProperty("genxml/hidden/modelid") == modelid);
                if (obj.Any()) return obj.First();
                return Models.First(); // assume first is required
            }
            return null;
        }

        public Double FromPrice()
        {
            Double fromprice = 0;
            foreach (var m in Models)
            {
                if ((fromprice == 0) || (fromprice > m.GetXmlPropertyDouble("genxml/textbox/txtunitcost"))) fromprice = m.GetXmlPropertyDouble("genxml/textbox/txtunitcost");
            }
            return fromprice;
        }

        public Double SalePrice()
        {
            Double saleprice = 0;
            foreach (var m in Models)
            {
                if ((saleprice == 0) || (saleprice > m.GetXmlPropertyDouble("genxml/textbox/txtsaleprice"))) saleprice = m.GetXmlPropertyDouble("genxml/textbox/txtsaleprice");
            }
            if (saleprice == 0) saleprice = FromPrice();
            return saleprice;
        }

        public Double BestPrice()
        {
            var bestprice = FromPrice();
            if (SalePrice() < bestprice) bestprice = SalePrice();
            return bestprice;
        }

        public Double DealerFromPrice()
        {
            Double fromprice = 0;
            foreach (var m in Models)
            {
                if ((fromprice == 0) || (fromprice > m.GetXmlPropertyDouble("genxml/textbox/txtdealercost"))) fromprice = m.GetXmlPropertyDouble("genxml/textbox/txtdealercost");
            }
            return fromprice;
        }

        public Double DealerSalePrice()
        {
            Double saleprice = 0;
            foreach (var m in Models)
            {
                if ((saleprice == 0) || (saleprice > m.GetXmlPropertyDouble("genxml/textbox/txtdealersale"))) saleprice = m.GetXmlPropertyDouble("genxml/textbox/txtdealersale");
            }
            if (saleprice == 0) saleprice = FromPrice();
            return saleprice;
        }

        public Double DealerBestPrice()
        {
            var bestprice = DealerFromPrice();
            if (DealerSalePrice() < bestprice) bestprice = DealerSalePrice();
            return bestprice;
        }

        public void AdjustModelQtyBy(String modelid,Double qty)
        {
            var model = GetModel(modelid);
            var newqty = DataRecord.GetXmlPropertyDouble("genxml/models/genxml[" + model.ItemID.ToString("") + "]/textbox/txtqtyremaining");
            newqty = newqty - qty;
            DataRecord.SetXmlPropertyDouble("genxml/models/genxml[" + model.ItemID.ToString("") + "]/textbox/txtqtyremaining", newqty);            
        }

        /// <summary>
        /// Update transient model qty.  
        /// When a order is places and redirected to the bank, the stock is placed into a transient status. So the stock is effectly calculated as being taken.
        /// On successful return from the bank the transient qty is removed from the model qty.  On payment failure, the stock is released after 20 mins
        /// </summary>
        /// <param name="modelid"></param>
        /// <param name="orderid"></param>
        /// <param name="addqty"></param>
        public void UpdateModelTransQty(String modelid,int orderid, Double addqty)
        {
            var key = "modeltrans*" + modelid + "*" + _portalId.ToString("");
            var l = (List<ModelTransData>)Utils.GetCache(key);
            if (l == null) l = new List<ModelTransData>();

            l.RemoveAll(s => s.setdate < DateTime.Now.AddMinutes(-20)); // remove timed out records
            var t = new ModelTransData();
            t.qty = addqty;
            t.orderid = orderid;
            t.modelid = modelid;
            t.setdate = DateTime.Now;
            l.Add(t);
            Utils.SetCache(key,l);
        }

        public Double GetModelTransQty(String modelid, int orderid)
        {
            var key = "modeltrans*" + modelid + "*" + Info.PortalId.ToString("");
            var l = (List<ModelTransData>)Utils.GetCache(key);
            if (l == null) return 0;
            Double tot = 0;
            foreach (var i in l)
            {
                if (i.setdate > DateTime.Now.AddMinutes(-20) && i.orderid != orderid)
                {
                    tot += i.qty;
                }
            }
            return tot;
        }

        public void ReleaseModelTransQty(String modelid, int orderid, Double addqty)
        {
            var key = "modeltrans*" + modelid + "*" + Info.PortalId.ToString("");
            var l = (List<ModelTransData>)Utils.GetCache(key);
            if (l != null)
            {
                l.RemoveAll(s => s.setdate < DateTime.Now.AddMinutes(-20)); // remove timed out records
                l.RemoveAll(s => s.orderid == orderid); // remove orderid
                Utils.SetCache(key, l);                
            }
        }

        public void ApplyModelTransQty(String modelid, int orderid, Double addqty)
        {
            AdjustModelQtyBy(modelid, addqty);
            Save();
            ReleaseModelTransQty(modelid, orderid, addqty);
        }


        public NBrightInfo GetOption(String optionid)
        {
            var obj = Options.Where(i => i.GetXmlProperty("genxml/hidden/optionid") == optionid);
            return obj.First();
        }

        public NBrightInfo GetOptionValue(String optionid, String optionvalueid)
        {
            var obj = OptionValues.Where(i => i.GetXmlProperty("genxml/hidden/optionid") == optionid && (i.GetXmlProperty("genxml/hidden/optionvalueid") == optionvalueid || optionvalueid == ""));
            return obj.First();
        }

        /// <summary>
        /// Select categories linked to product, by groupref
        /// </summary>
        /// <param name="groupref">groupref for select, "" = all, "cat"= Category only, "!cat" = all non-category, "{groupref}"=this group only</param>
        /// <param name="cascade">get all cascade records to get all parent categories</param>
        /// <returns></returns>
        public List<GroupCategoryData> GetCategories(String groupref = "",Boolean cascade = false)
        {
            if (Info == null) return new List<GroupCategoryData>(); // stop throwing an error no product exists,

            var objGrpCtrl = new GrpCatController(_lang);
            var catl = objGrpCtrl.GetProductCategories(Info.ItemID, groupref, cascade);
            if (Utils.IsNumeric(DataRecord.GetXmlProperty("genxml/defaultcatid")) && catl.Count > 0)
            {
                var objl = catl.Where(i => i.isdefault == true);
                foreach (var i in objl)
                {
                    i.isdefault = false;
                }
                var dcatid = Convert.ToInt32(DataRecord.GetXmlProperty("genxml/defaultcatid"));
                var obj = catl.Where(i => i.categoryid == dcatid);
                var groupCategoryDatas = obj as GroupCategoryData[] ?? obj.ToArray();
                if (groupCategoryDatas.Any()) groupCategoryDatas.First().isdefault = true;
            }
            return catl;
        }

        /// <summary>
        /// Select properties linked to product, by groupref
        /// </summary>
        /// <param name="groupref">groupref for select, "" = all, "cat"= Category only, "!cat" = all non-category, "{groupref}"=this group only</param>
        /// <param name="cascade">get all cascade records to get all parent categories</param>
        /// <returns></returns>
        public List<GroupCategoryData> GetProperties(String groupref = "", Boolean cascade = false)
        {
            return GetCategories(groupref, cascade);
        }

        public GroupCategoryData GetDefaultCategory()
        {
            if (Utils.IsNumeric(DataRecord.GetXmlProperty("genxml/defaultcatid")))
            {
                var objGrpCtrl = new GrpCatController(_lang);
                var obj = objGrpCtrl.GetCategory(Convert.ToInt32(DataRecord.GetXmlProperty("genxml/defaultcatid")));
                if (obj != null) return obj;
            }
            var catl = GetCategories();
            if (catl.Any()) return catl[0];
            return null;
        }

        public void SetDefaultCategory(int categoryid)
        {
            var objCtrl = new NBrightBuyController();
            DataRecord.SetXmlProperty("genxml/defaultcatid", categoryid.ToString());
            objCtrl.Update(DataRecord);
        }


        public Boolean HasProperty(String propertyref)
        {
            var objGrpCtrl = new GrpCatController(_lang);
            var l = objGrpCtrl.GetProductCategories(Info.ItemID, "!cat");
            return l.Any(i => (i.categoryref == propertyref || i.propertyref == propertyref));
        }

        public Boolean IsInCategory(String categoryref)
        {
            var objGrpCtrl = new GrpCatController(_lang);
            var l = objGrpCtrl.GetProductCategories(Info.ItemID, "cat",true);
            return l.Any(i => i.categoryref == categoryref);
        }

        public List<NBrightInfo> GetRelatedProducts()
        {
            var objCtrl = new NBrightBuyController();
            var strSelectedIds = "";
            var arylist = objCtrl.GetList(_portalId, -1, _typeCode + "XREF", " and NB1.parentitemid = " + Info.ItemID.ToString(""));
            foreach (var obj in arylist)
            {
                strSelectedIds += obj.XrefItemId.ToString("") + ",";
            }
            var relList = new List<NBrightInfo>();
            if (strSelectedIds.TrimEnd(',') != "")
            {
                var strFilter = " and NB1.[ItemId] in (" + strSelectedIds.TrimEnd(',') + ") ";
                relList = objCtrl.GetDataList(_portalId, -1, _typeCode, _typeLangCode, _lang, strFilter, "");
            }
            return relList;
        }

        public List<NBrightInfo> GetClients()
        {
            if (Info == null) return new List<NBrightInfo>();
            var objCtrl = new NBrightBuyController();
            var userlist = objCtrl.GetDnnUserProductClient(_portalId, Info.ItemID);
            return userlist;
        }

        public Boolean HasClient(int userId)
        {
            var l = GetClients();
            foreach (var u in l)
            {
                // usewrid from DB is past back in ItemId column, so test userid against that.
                if (u.ItemID == userId) return true;
            }

            return false;
        }

        /// <summary>
        /// Save all Product Data
        /// </summary>
        /// <param name="triggerevent"> Used to stop infinate loop when Save used in event method</param>
        public void Save(bool triggerevent = true)
        {
            foreach (var model in Models)
            {
                var qty = DataRecord.GetXmlPropertyDouble("genxml/models/genxml[" + model.ItemID.ToString("") + "]/textbox/txtqtyremaining");
                var minqty = DataRecord.GetXmlPropertyDouble("genxml/models/genxml[" + model.ItemID.ToString("") + "]/textbox/txtqtyminstock");
                var currentstatus = DataRecord.GetXmlProperty("genxml/models/genxml[" + model.ItemID.ToString("") + "]/dropdownlist/modelstatus");
                var activatestock = DataRecord.GetXmlPropertyBool("genxml/models/genxml[" + model.ItemID.ToString("") + "]/checkbox/chkstockon");
                if (activatestock && currentstatus != "040" && currentstatus != "050")
                {
                    if (qty > 0) DataRecord.SetXmlProperty("genxml/models/genxml[" + model.ItemID.ToString("") + "]/dropdownlist/modelstatus", "010");
                    if (minqty == qty) DataRecord.SetXmlProperty("genxml/models/genxml[" + model.ItemID.ToString("") + "]/dropdownlist/modelstatus", "020");
                    if (qty <= 0 && minqty < qty) DataRecord.SetXmlProperty("genxml/models/genxml[" + model.ItemID.ToString("") + "]/dropdownlist/modelstatus", "030");
                }
                else
                {
                    // stock not activate so set status to available
                    DataRecord.SetXmlProperty("genxml/models/genxml[" + model.ItemID.ToString("") + "]/dropdownlist/modelstatus", "010");
                }

                if (DataRecord.GetXmlProperty("genxml/models/genxml[" + model.ItemID.ToString("") + "]/hidden/modelid") == "") DataRecord.SetXmlProperty("genxml/models/genxml[" + model.ItemID.ToString("") + "]/hidden/modelid", Utils.GetUniqueKey());
            }

            // calcs what the import and guidkey should be.
            SetGuidKey();

            DataRecord.ModuleId = -1; // moduleid of product always set to -1 (Portal Wide)
            DataLangRecord.ModuleId = -1; // moduleid of product always set to -1 (Portal Wide)

            var objCtrl = new NBrightBuyController();
            var productid = objCtrl.Update(DataRecord);
            DataLangRecord.ParentItemId = productid;
            var plangid = objCtrl.Update(DataLangRecord);

            // reload data so it's upto date with new ids
            DataRecord = objCtrl.Get(productid); 
            DataLangRecord = objCtrl.Get(plangid);

            if (triggerevent)
            {
                NBrightBuyUtils.ProcessEventProvider(EventActions.AfterProductSave, DataRecord);
                // reload data so if event has altered data we use that.
                DataRecord = objCtrl.Get(productid);
                DataLangRecord = objCtrl.Get(plangid);
            }
        }

        public void Delete()
        {
            // remove any cache
            ProductUtils.RemoveProductDataCache(DataRecord.PortalId, DataRecord.ItemID);
            //delete and allow DB to cascade delete
            var objCtrl = new NBrightBuyController();
            objCtrl.Delete(DataRecord.ItemID);
        }

        public void Update(String xmlData)
        {
            var info = new NBrightInfo();
            info.ItemID = -1;
            info.TypeCode = "UPDATEDATA";
            info.XMLData = xmlData;

            //check we have valid strutre for XML
            DataRecord.ValidateXmlFormat();
            DataLangRecord.ValidateXmlFormat();

            var updatefields = new List<String>();
            var localfields = info.GetXmlProperty("genxml/hidden/localizedfields").Split(',');
            foreach (var f in localfields) { updatefields.Add(f); }
            // merge custom field into update
            var localproductfields = info.GetXmlProperty("genxml/hidden/localizedproductfields").Split(',');
            foreach (var f in localproductfields){updatefields.Add(f);}

            foreach (var f in updatefields)
            {
                if (f != "")
                {
                    if (f == "genxml/edt/description")
                    {
                        // special processing for editor, to place code in standard place.
                        if (DataLangRecord.XMLDoc.SelectSingleNode("genxml/edt") == null) DataLangRecord.AddSingleNode("edt", "", "genxml");
                        if (info.GetXmlProperty("genxml/textbox/description") == "")
                            DataLangRecord.SetXmlProperty(f, info.GetXmlProperty("genxml/edt/description"));
                        else
                            DataLangRecord.SetXmlProperty(f, info.GetXmlProperty("genxml/textbox/description")); // ajax on ckeditor (Ajax doesn't work for telrik)
                    }
                    else
                    {
                        DataLangRecord.RemoveXmlNode(f);
                        var xpathDest = f.Split('/');
                        if (xpathDest.Count() >= 2) DataLangRecord.AddXmlNode(xmlData, f, xpathDest[0] + "/" + xpathDest[1]);
                    }
                        DataLangRecord.SetXmlProperty(f, info.GetXmlProperty(f));

                    DataRecord.RemoveXmlNode(f);
                }
            }

            updatefields = new List<String>();
            var fields = info.GetXmlProperty("genxml/hidden/fields").Split(',');
            foreach (var f in fields) { updatefields.Add(f); }
            // merge custom field into update
            var productfields = info.GetXmlProperty("genxml/hidden/productfields").Split(',');
            foreach (var f in productfields) { updatefields.Add(f); }

            foreach (var f in updatefields)
            {
                if (f != "")
                {
                    //DataRecord.RemoveXmlNode(f);
                    //var xpathDest = f.Split('/');
                    //if (xpathDest.Count() >= 2) DataRecord.AddXmlNode(xmlData, f, xpathDest[0] + "/" + xpathDest[1]);
                    DataRecord.SetXmlProperty(f, info.GetXmlProperty(f));

                    // if we have a image field then we need to create the imageurl field
                    if (info.GetXmlProperty(f.Replace("textbox/", "hidden/hidinfo")) == "Img=True")
                        DataRecord.SetXmlProperty(f.Replace("textbox/", "hidden/") + "url", _storeSettings.FolderImages + "/" + info.GetXmlProperty(f.Replace("textbox/", "hidden/hid")));

                    DataLangRecord.RemoveXmlNode(f);
                }
            }

            // update Models
            var strXml = info.GetXmlProperty("genxml/hidden/xmlupdatemodeldata");
            strXml = GenXmlFunctions.DecodeCDataTag(strXml);
            UpdateModels(strXml);
            // update Options
            strXml = info.GetXmlProperty("genxml/hidden/xmlupdateproductoptions");
            strXml = GenXmlFunctions.DecodeCDataTag(strXml);
            UpdateOptions(strXml);
            // update Options
            strXml = info.GetXmlProperty("genxml/hidden/xmlupdateproductoptionvalues");
            strXml = GenXmlFunctions.DecodeCDataTag(strXml);
            UpdateOptionValues(strXml);
            // update images
            UpdateImages(info);
            // update docs
            UpdateDocs(info);

            IsOnSale = CheckIsOnSale();
            DealerIsOnSale = DealerCheckIsOnSale();
            IsInStock = CheckIsInStock();
            ClientFileUpload = CheckClientFileUpload();

        }

        public void UpdateDocs(NBrightInfo info)
        {
            var strAjaxXml = info.GetXmlProperty("genxml/hidden/xmlupdateproductdocs");
            strAjaxXml = GenXmlFunctions.DecodeCDataTag(strAjaxXml);
            var docList = NBrightBuyUtils.GetGenXmlListByAjax(strAjaxXml, "");            
            UpdateDocs(docList);

            //add new doc if uploaded
            var docFile = info.GetXmlProperty("genxml/hidden/hiddocument");
            if (docFile != "")
            {
                var postedFile = info.GetXmlProperty("genxml/hidden/posteddocumentname");
                AddNewDoc(_storeSettings.FolderDocumentsMapPath.TrimEnd('\\') + "\\" + docFile, postedFile);
            }

        }

        public void UpdateDocs(List<NBrightInfo> docList)
        {
            // build xml for data records
            var strXml = "<genxml><docs>";
            var strXmlLang = "<genxml><docs>";
            foreach (var docInfo in docList)
            {
                var objInfo = new NBrightInfo(true);
                var objInfoLang = new NBrightInfo(true);

                var localfields = docInfo.GetXmlProperty("genxml/hidden/localizedfields").Split(',');
                foreach (var f in localfields.Where(f => f != ""))
                {
                    objInfoLang.SetXmlProperty(f, docInfo.GetXmlProperty(f));
                }
                strXmlLang += objInfoLang.XMLData;

                var fields = docInfo.GetXmlProperty("genxml/hidden/fields").Split(',');
                foreach (var f in fields.Where(f => f != ""))
                {
                    objInfo.SetXmlProperty(f, docInfo.GetXmlProperty(f));
                    if (f == "genxml/hidden/filepath")
                    {
                        // save relitive path also
                        var fname = Path.GetFileName(docInfo.GetXmlProperty(f));
                        objInfo.SetXmlProperty("genxml/hidden/filename", fname);
                        objInfo.SetXmlProperty("genxml/hidden/filerelpath", StoreSettings.Current.FolderDocuments + "/" + fname);
                    }
                }
                strXml += objInfo.XMLData;
            }
            strXml += "</docs></genxml>";
            strXmlLang += "</docs></genxml>";

            // replace models xml 
            DataRecord.ReplaceXmlNode(strXml, "genxml/docs", "genxml");
            DataLangRecord.ReplaceXmlNode(strXmlLang, "genxml/docs", "genxml");

        }

        public void UpdateImages(NBrightInfo info)
        {
            var strAjaxXml = info.GetXmlProperty("genxml/hidden/xmlupdateproductimages");
            strAjaxXml = GenXmlFunctions.DecodeCDataTag(strAjaxXml);
            var imgList = NBrightBuyUtils.GetGenXmlListByAjax(strAjaxXml, "");
            UpdateImages(imgList);

            //add new image if uploaded
            var imgFile = info.GetXmlProperty("genxml/hidden/hidimage");
            if (imgFile != "")
            {
                AddNewImage(_storeSettings.FolderImages.TrimEnd('/') + "/" + imgFile, _storeSettings.FolderImagesMapPath.TrimEnd('\\') + "\\" + imgFile);
            }

        }

        public void UpdateImages(List<NBrightInfo> imgList)
        {
            // build xml for data records
            var strXml = "<genxml><imgs>";
            var strXmlLang = "<genxml><imgs>";
            foreach (var imgInfo in imgList)
            {
                var objInfo = new NBrightInfo(true);
                var objInfoLang = new NBrightInfo(true);

                var localfields = imgInfo.GetXmlProperty("genxml/hidden/localizedfields").Split(',');
                foreach (var f in localfields.Where(f => f != ""))
                {
                    objInfoLang.SetXmlProperty(f, imgInfo.GetXmlProperty(f));
                }
                strXmlLang += objInfoLang.XMLData;

                var fields = imgInfo.GetXmlProperty("genxml/hidden/fields").Split(',');
                foreach (var f in fields.Where(f => f != ""))
                {
                    objInfo.SetXmlProperty(f, imgInfo.GetXmlProperty(f));
                }
                strXml += objInfo.XMLData;
            }
            strXml += "</imgs></genxml>";
            strXmlLang += "</imgs></genxml>";

            // replace models xml 
            DataRecord.ReplaceXmlNode(strXml, "genxml/imgs", "genxml");
            DataLangRecord.ReplaceXmlNode(strXmlLang, "genxml/imgs", "genxml");

        }

        public void UpdateModels(String xmlAjaxData)
        {
            var modelList = NBrightBuyUtils.GetGenXmlListByAjax(xmlAjaxData, "");

            // build xml for data records
            var strXml = "<genxml><models>";
            var strXmlLang = "<genxml><models>";
            foreach (var modelInfo in modelList)
            {
                var objInfo = new NBrightInfo(true);
                var objInfoLang = new NBrightInfo(true);

                var localfields = modelInfo.GetXmlProperty("genxml/hidden/localizedfields").Split(',');
                foreach (var f in localfields.Where(f => f != ""))
                {
                    var datatype = modelInfo.GetXmlProperty(f + "/@datatype");
                    if (datatype == "date")
                        objInfoLang.SetXmlProperty(f, modelInfo.GetXmlProperty(f), TypeCode.DateTime);
                    else if (datatype == "double")
                        objInfoLang.SetXmlProperty(f, modelInfo.GetXmlPropertyDouble(f).ToString(""), TypeCode.Double);
                    else
                        objInfoLang.SetXmlProperty(f, modelInfo.GetXmlProperty(f));
                }
                strXmlLang += objInfoLang.XMLData;

                var fields = modelInfo.GetXmlProperty("genxml/hidden/fields").Split(',');
                foreach (var f in fields.Where(f => f != ""))
                {
                    var datatype = modelInfo.GetXmlProperty(f + "/@datatype");
                    if (datatype == "date")
                        objInfo.SetXmlProperty(f, modelInfo.GetXmlProperty(f), TypeCode.DateTime);
                    else if (datatype == "double")
                        objInfo.SetXmlProperty(f, modelInfo.GetXmlPropertyDouble(f).ToString(""), TypeCode.Double);
                    else
                        objInfo.SetXmlProperty(f, modelInfo.GetXmlProperty(f));
                }
                strXml += objInfo.XMLData;
            }
            strXml += "</models></genxml>";
            strXmlLang += "</models></genxml>";

            // replace models xml 
            DataRecord.ReplaceXmlNode(strXml, "genxml/models", "genxml");
            DataLangRecord.ReplaceXmlNode(strXmlLang, "genxml/models", "genxml");
        }

        public void UpdateOptions(String xmlAjaxData)
        {
            var objList = NBrightBuyUtils.GetGenXmlListByAjax(xmlAjaxData, "");

            // build xml for data records
            var strXml = "<genxml><options>";
            var strXmlLang = "<genxml><options>";
            foreach (var objDataInfo in objList)
            {
                var objInfo = new NBrightInfo(true);
                var objInfoLang = new NBrightInfo(true);

                var localfields = objDataInfo.GetXmlProperty("genxml/hidden/localizedfields").Split(',');
                foreach (var f in localfields.Where(f => f != ""))
                {
                    var datatype = objDataInfo.GetXmlProperty(f + "/@datatype");
                    if (datatype == "date")
                        objInfoLang.SetXmlProperty(f, objDataInfo.GetXmlProperty(f), TypeCode.DateTime);
                    else if (datatype == "double")
                        objInfoLang.SetXmlProperty(f, objDataInfo.GetXmlPropertyDouble(f).ToString(""), TypeCode.Double);
                    else
                        objInfoLang.SetXmlProperty(f, objDataInfo.GetXmlProperty(f));
                }
                strXmlLang += objInfoLang.XMLData;

                var fields = objDataInfo.GetXmlProperty("genxml/hidden/fields").Split(',');
                foreach (var f in fields.Where(f => f != ""))
                {
                    var datatype = objDataInfo.GetXmlProperty(f + "/@datatype");
                    if (datatype == "date")
                        objInfo.SetXmlProperty(f, objDataInfo.GetXmlProperty(f), TypeCode.DateTime);
                    else if (datatype == "double")
                        objInfo.SetXmlProperty(f, objDataInfo.GetXmlPropertyDouble(f).ToString(""), TypeCode.Double);
                    else
                        objInfo.SetXmlProperty(f, objDataInfo.GetXmlProperty(f));
                }
                strXml += objInfo.XMLData;
            }
            strXml += "</options></genxml>";
            strXmlLang += "</options></genxml>";

            // replace models xml 
            DataRecord.ReplaceXmlNode(strXml, "genxml/options", "genxml");
            DataLangRecord.ReplaceXmlNode(strXmlLang, "genxml/options", "genxml");        
        }

        public void UpdateOptionValues(String xmlAjaxData)
        {
            var objList = NBrightBuyUtils.GetGenXmlListByAjax(xmlAjaxData, "");
            if (objList != null)
            {
                if (objList.Count == 0)
                {
                    // no optionvalues, so remove all 
                    var nodList = DataRecord.XMLDoc.SelectNodes("genxml/optionvalues");
                    if (nodList != null)
                        for (int i = nodList.Count - 1; i >= 0; i--)
                        {
                            var parentNode = nodList[i].ParentNode;
                            if (parentNode != null) parentNode.RemoveChild(nodList[i]);
                        }
                    nodList = DataLangRecord.XMLDoc.SelectNodes("genxml/optionvalues");
                    if (nodList != null)
                        for (int i = nodList.Count - 1; i >= 0; i--)
                        {
                            var parentNode = nodList[i].ParentNode;
                            if (parentNode != null) parentNode.RemoveChild(nodList[i]);
                        }
                }
                else
                {

                    // get a list of optionid that need to be processed
                    var distinctOptionIds = new Dictionary<String, String>();
                    foreach (var o in objList)
                    {
                        if (!distinctOptionIds.ContainsKey(o.GetXmlProperty("genxml/hidden/optionid")))
                            distinctOptionIds.Add(o.GetXmlProperty("genxml/hidden/optionid"),
                                o.GetXmlProperty("genxml/hidden/optionid"));
                    }

                    foreach (var optid in distinctOptionIds.Keys)
                    {
                        // build xml for data records
                        var strXml = "<genxml><optionvalues optionid='" + optid + "'>";
                        var strXmlLang = "<genxml><optionvalues optionid='" + optid + "'>";
                        foreach (var objDataInfo in objList)
                        {
                            if (objDataInfo.GetXmlProperty("genxml/hidden/optionid") == optid)
                            {
                                var objInfo = new NBrightInfo(true);
                                var objInfoLang = new NBrightInfo(true);

                                var localfields = objDataInfo.GetXmlProperty("genxml/hidden/localizedfields").Split(',');
                                foreach (var f in localfields.Where(f => f != ""))
                                {
                                    var datatype = objDataInfo.GetXmlProperty(f + "/@datatype");
                                    if (datatype == "date")
                                        objInfoLang.SetXmlProperty(f, objDataInfo.GetXmlProperty(f), TypeCode.DateTime);
                                    else if (datatype == "double")
                                        objInfoLang.SetXmlProperty(f, objDataInfo.GetXmlPropertyDouble(f).ToString(""), TypeCode.Double);
                                    else
                                    objInfoLang.SetXmlProperty(f, objDataInfo.GetXmlProperty(f));
                                }
                                strXmlLang += objInfoLang.XMLData;

                                var fields = objDataInfo.GetXmlProperty("genxml/hidden/fields").Split(',');
                                foreach (var f in fields.Where(f => f != ""))
                                {
                                    var datatype = objDataInfo.GetXmlProperty(f + "/@datatype");
                                    if (datatype == "date")
                                        objInfo.SetXmlProperty(f, objDataInfo.GetXmlProperty(f), TypeCode.DateTime);
                                    else if (datatype == "double")
                                        objInfo.SetXmlProperty(f, objDataInfo.GetXmlPropertyDouble(f).ToString(""), TypeCode.Double);
                                    else
                                    objInfo.SetXmlProperty(f, objDataInfo.GetXmlProperty(f));
                                }
                                strXml += objInfo.XMLData;
                            }
                        }
                        strXml += "</optionvalues></genxml>";
                        strXmlLang += "</optionvalues></genxml>";

                        // replace  xml 
                        DataRecord.ReplaceXmlNode(strXml, "genxml/optionvalues[@optionid='" + optid + "']", "genxml");
                        DataLangRecord.ReplaceXmlNode(strXmlLang, "genxml/optionvalues[@optionid='" + optid + "']",
                            "genxml");
                    }

                    // tidy up any invlid option values (usually created in a migration phase)
                    var nodList = DataRecord.XMLDoc.SelectNodes("genxml/options/genxml");
                    var optionids = new Dictionary<String, String>();
                    if (nodList != null)
                        foreach (XmlNode nod in nodList)
                        {
                            var selectSingleNode = nod.SelectSingleNode("hidden/optionid");
                            if (selectSingleNode != null)
                                optionids.Add(selectSingleNode.InnerText, selectSingleNode.InnerText);
                        }
                    nodList = DataRecord.XMLDoc.SelectNodes("genxml/optionvalues");
                    if (nodList != null)
                        foreach (XmlNode nod in nodList)
                        {
                            if (nod.Attributes != null && nod.Attributes["optionid"] != null)
                            {
                                if (!optionids.ContainsKey(nod.Attributes["optionid"].InnerText))
                                {
                                    DataRecord.RemoveXmlNode("genxml/optionvalues[@optionid='" +
                                                             nod.Attributes["optionid"].InnerText + "']");
                                    DataLangRecord.RemoveXmlNode("genxml/optionvalues[@optionid='" +
                                                                 nod.Attributes["optionid"].InnerText + "']");
                                }
                            }
                        }
                }
            }
        }

        public String AddNewOptionValue(String optionid)
        {
            var newkey = Utils.GetUniqueKey();
            var strXml = "<genxml><optionvalues optionid='" + optionid + "'><genxml><hidden><optionid>" + optionid + "</optionid><optionvalueid>" + newkey + "</optionvalueid></hidden></genxml></optionvalues></genxml>";
            if (DataRecord.XMLDoc.SelectSingleNode("genxml/optionvalues[@optionid='" + optionid + "']") == null)
            {
                DataRecord.AddXmlNode(strXml, "genxml/optionvalues[@optionid='" + optionid + "']", "genxml");
            }
            else
            {
                DataRecord.AddXmlNode(strXml, "genxml/optionvalues[@optionid='" + optionid + "']/genxml", "genxml/optionvalues[@optionid='" + optionid + "']");
            }
            return newkey;
        }

        public String AddNewOption()
        {
            var newkey = Utils.GetUniqueKey();
            var strXml = "<genxml><options><genxml><new>new</new><hidden><optionid>" + newkey + "</optionid></hidden></genxml></options></genxml>";
            if (DataRecord.XMLDoc.SelectSingleNode("genxml/options") == null)
            {
                DataRecord.AddXmlNode(strXml, "genxml/options", "genxml");
            }
            else
            {
                DataRecord.AddXmlNode(strXml, "genxml/options/genxml", "genxml/options");
            }
            return newkey;
        }

        public String AddNewModel()
        {
            var newkey = Utils.GetUniqueKey();
            var strXml = "<genxml><models><genxml><hidden><modelid>" + newkey + "</modelid></hidden><textbox/><checkbox/><checkboxlist/><radiobuttonlist/><dropdownlist/></genxml></models></genxml>";
            if (DataRecord.XMLDoc.SelectSingleNode("genxml/models") == null)
            {
                DataRecord.AddXmlNode(strXml, "genxml/models", "genxml");
                DataLangRecord.AddXmlNode(strXml, "genxml/models", "genxml");
            }
            else
            {
                DataRecord.AddXmlNode(strXml, "genxml/models/genxml", "genxml/models");
                DataLangRecord.AddXmlNode(strXml, "genxml/models/genxml", "genxml/models");
            }

            var modellp = Models.Count + 1;
            if (DataLangRecord != null && DataLangRecord.GetXmlProperty("genxml/models/genxml[" + modellp + "]/textbox/txtmodelname") == "")
            {
                DataLangRecord.SetXmlProperty("genxml/models/genxml[" + modellp + "]/textbox/txtmodelname", ProductName);
            }
            if (DataRecord.GetXmlProperty("genxml/models/genxml[" + modellp + "]/textbox/txtmodelref") == "")
            {
                DataRecord.SetXmlProperty("genxml/models/genxml[" + modellp + "]/textbox/txtmodelref", ProductRef);
            }


            return newkey;
        }

        public void AddNewImage(String imageurl, String imagepath)
        {
            var strXml = "<genxml><imgs><genxml><hidden><imagepath>" + imagepath + "</imagepath><imageurl>" + imageurl + "</imageurl></hidden></genxml></imgs></genxml>";
            if (DataRecord.XMLDoc.SelectSingleNode("genxml/imgs") == null)
            {
                DataRecord.AddXmlNode(strXml, "genxml/imgs", "genxml");
            }
            else
            {
                DataRecord.AddXmlNode(strXml, "genxml/imgs/genxml", "genxml/imgs");
            }
        }

        public void AddNewDoc(String docpath, String postedFileName)
        {

            var strXml = "<genxml><docs><genxml><hidden><filepath>" + docpath + "</filepath><fileext>" + Path.GetExtension(postedFileName) + "</fileext></hidden><textbox><txtfilename>" + postedFileName + "</txtfilename></textbox></genxml></docs></genxml>";
            if (DataRecord.XMLDoc.SelectSingleNode("genxml/docs") == null)
            {
                DataRecord.AddXmlNode(strXml, "genxml/docs", "genxml");
            }
            else
            {
                DataRecord.AddXmlNode(strXml, "genxml/docs/genxml", "genxml/docs");
            }
        }

        public void AddProperty(String propertyref)
        {
            var objCtrl = new NBrightBuyController();
            var pinfo = objCtrl.GetByGuidKey(_portalId, -1,"CATEGORY",propertyref);
            if (pinfo == null)
            {
                // not using the unique ref, look for the friendly propertyref name.
                var l = objCtrl.GetList(_portalId, -1, "CATEGORY", " and [XMLData].value('(genxml/textbox/propertyref)[1]','nvarchar(max)') = '" + propertyref + "' ", "", 1);
                if (l.Any()) pinfo = l[0];
            }
            if (pinfo != null && Utils.IsNumeric(pinfo.ItemID) && pinfo.ItemID > 0) AddCategory(pinfo.ItemID);
        }

        public void AddCategory(int categoryid)
        {
            if (Info != null)
            {
                var strGuid = categoryid.ToString("") + "x" + Info.ItemID.ToString("");
                var objCtrl = new NBrightBuyController();
                var nbi = objCtrl.GetByGuidKey(_portalId, -1, "CATXREF", strGuid);
                if (nbi == null)
                {
                    nbi = new NBrightInfo();
                    nbi.ItemID = -1;
                    nbi.PortalId = _portalId;
                    nbi.ModuleId = -1;
                    nbi.TypeCode = "CATXREF";
                    nbi.XrefItemId = categoryid;
                    nbi.ParentItemId = Info.ItemID;
                    nbi.XMLData = null;
                    nbi.TextData = null;
                    nbi.Lang = null;
                    nbi.GUIDKey = strGuid;
                    var newitemid = objCtrl.Update(nbi);
                    nbi = objCtrl.Get(newitemid);
                    nbi.XMLData = "<genxml><sort>" + newitemid.ToString() + "</sort></genxml>";
                    objCtrl.Update(nbi);

                    //add all cascade xref 
                    var objGrpCtrl = new GrpCatController(_lang, true);
                    var parentcats = objGrpCtrl.GetCategory(categoryid);
                    if (parentcats != null)
                    {
                        foreach (var p in parentcats.Parents)
                        {
                            strGuid = p.ToString("") + "x" + Info.ItemID.ToString("");
                            var obj = objCtrl.GetByGuidKey(_portalId, -1, "CATCASCADE", strGuid);
                            if (obj == null)
                            {
                                nbi = new NBrightInfo();
                                nbi.ItemID = -1;
                                nbi.PortalId = _portalId;
                                nbi.ModuleId = -1;
                                nbi.XrefItemId = p;
                                nbi.ParentItemId = Info.ItemID;
                                nbi.TypeCode = "CATCASCADE";
                                nbi.GUIDKey = strGuid;
                                newitemid = objCtrl.Update(nbi);
                                nbi = objCtrl.Get(newitemid);
                                nbi.XMLData = "<genxml><sort>" + newitemid.ToString() + "</sort></genxml>";
                                objCtrl.Update(nbi);
                            }
                        }
                    }
                }                
            }
        }

        public void RemoveCategory(int categoryid)
        {
            var objCtrl = new NBrightBuyController();
            if (Utils.IsNumeric(DataRecord.GetXmlProperty("genxml/defaultcatid")) && categoryid == Convert.ToInt32(DataRecord.GetXmlProperty("genxml/defaultcatid")))
            {
                DataRecord.SetXmlProperty("genxml/defaultcatid", "");
                objCtrl.Update(DataRecord);
            }

            var parentitemid = Info.ItemID.ToString("");
            var xrefitemid = categoryid.ToString("");
            var objQual = DotNetNuke.Data.DataProvider.Instance().ObjectQualifier;
            var dbOwner = DotNetNuke.Data.DataProvider.Instance().DatabaseOwner;
            var stmt = "delete from " + dbOwner + "[" + objQual + "NBrightBuy] where typecode = 'CATXREF' and XrefItemId = " + xrefitemid + " and parentitemid = " + parentitemid;
            objCtrl.ExecSql(stmt);
            //remove all cascade xref 
            var objGrpCtrl = new GrpCatController(_lang, true);
            var parentcats = objGrpCtrl.GetCategory(Convert.ToInt32(xrefitemid));
            if (parentcats != null)
            {
                foreach (var p in parentcats.Parents)
                {
                    var xreflist = objCtrl.GetList(_portalId, -1, "CATXREF", " and NB1.parentitemid = " + parentitemid);
                    if (xreflist != null)
                    {
                        var deleterecord = true;
                        foreach (var xref in xreflist)
                        {
                            var catid = xref.XrefItemId;
                            var xrefparentcats = objGrpCtrl.GetCategory(Convert.ToInt32(catid));
                            if (xrefparentcats != null && xrefparentcats.Parents.Contains(p))
                            {
                                deleterecord = false;
                                break;
                            }
                        }
                        if (deleterecord)
                        {
                            stmt = "delete from " + dbOwner + "[" + objQual + "NBrightBuy] where typecode = 'CATCASCADE' and XrefItemId = " + p.ToString("") + " and parentitemid = " + parentitemid;
                            objCtrl.ExecSql(stmt);
                        }

                    }
                }
            }

        }

        public void AddRelatedProduct(int productid)
        {
            if (productid!= Info.ItemID)  //cannot be related to itself
            {
                var strGuid = productid.ToString("") + "x" + Info.ItemID.ToString("");
                var objCtrl = new NBrightBuyController();
                var nbi = objCtrl.GetByGuidKey(_portalId, -1, _typeCode + "XREF", strGuid);
                if (nbi == null)
                {
                    nbi = new NBrightInfo();
                    nbi.ItemID = -1;
                    nbi.PortalId = _portalId;
                    nbi.ModuleId = -1;
                    nbi.TypeCode = _typeCode + "XREF";
                    nbi.XrefItemId = productid;
                    nbi.ParentItemId = Info.ItemID;
                    nbi.XMLData = null;
                    nbi.TextData = null;
                    nbi.Lang = null;
                    nbi.GUIDKey = strGuid;
                    objCtrl.Update(nbi);
                }                
            }
        }

        public void RemoveRelatedProduct(int productid)
        {
            var parentitemid = Info.ItemID.ToString("");
            var xrefitemid = productid.ToString("");
            var objCtrl = new NBrightBuyController();
            var objQual = DotNetNuke.Data.DataProvider.Instance().ObjectQualifier;
            var dbOwner = DotNetNuke.Data.DataProvider.Instance().DatabaseOwner;
            var stmt = "delete from " + dbOwner + "[" + objQual + "NBrightBuy] where typecode = '" + _typeCode + "XREF' and XrefItemId = " + xrefitemid + " and parentitemid = " + parentitemid;
            objCtrl.ExecSql(stmt);
        }

        public void AddClient(int userid)
        {
            var strGuid = userid.ToString("") + "x" + Info.ItemID.ToString("");
            var objCtrl = new NBrightBuyController();
            var nbi = objCtrl.GetByGuidKey(_portalId, -1, "USERPRDXREF", strGuid);
            if (nbi == null)
            {
                nbi = new NBrightInfo();
                nbi.ItemID = -1;
                nbi.PortalId = _portalId;
                nbi.ModuleId = -1;
                nbi.TypeCode = "USERPRDXREF";
                nbi.XrefItemId = 0;
                nbi.ParentItemId = Info.ItemID;
                nbi.TextData = null;
                nbi.Lang = null;
                nbi.UserId = userid;
                nbi.GUIDKey = strGuid;
                objCtrl.Update(nbi);
            }
        }

        public void RemoveClient(int userid)
        {
            var parentitemid = Info.ItemID.ToString("");
            var objCtrl = new NBrightBuyController();
            var objQual = DotNetNuke.Data.DataProvider.Instance().ObjectQualifier;
            var dbOwner = DotNetNuke.Data.DataProvider.Instance().DatabaseOwner;
            var stmt = "delete from " + dbOwner + "[" + objQual + "NBrightBuy] where typecode = 'USERPRDXREF' and UserId = " + userid.ToString("") + " and parentitemid = " + parentitemid;
            objCtrl.ExecSql(stmt);
        }


        public void ResetLanguage(String resetToLang)
        {
            if (resetToLang != DataLangRecord.Lang)
            {
                var resetToLangData = ProductUtils.GetProductData(DataRecord.ItemID, resetToLang);
                var objCtrl = new NBrightBuyController();
                DataLangRecord.XMLData = resetToLangData.DataLangRecord.XMLData;
                objCtrl.Update(DataLangRecord);                
            }
        }

        public int Validate()
        {
            var errorcount = 0;
            var upd = false;

            var objCtrl = new NBrightBuyController();

            SetGuidKey();
            objCtrl.Update(DataRecord);


            DataRecord.ValidateXmlFormat();
            if (DataLangRecord == null)
            {
                // we have no datalang record for this language, so get an existing one and save it.
                var l = objCtrl.GetList(_portalId, -1, _typeLangCode, " and NB1.ParentItemId = " + Info.ItemID.ToString(""));
                if (l.Count > 0)
                    DataLangRecord = (NBrightInfo) l[0].Clone();
                else
                    DataLangRecord = new NBrightInfo(true);

                DataLangRecord.ItemID = -1;
                DataLangRecord.ValidateXmlFormat();
                DataLangRecord.TypeCode = _typeLangCode;
                DataLangRecord.ParentItemId = Info.ItemID;
                DataLangRecord.Lang = _lang;
                objCtrl.Update(DataLangRecord);
            }

            // if we have no images search for a default image matching the product ref.
            if (ProductRef != "" && Imgs.Count == 0)
            {
                if (File.Exists(_storeSettings.FolderImagesMapPath.TrimEnd('\\') + "\\" + ProductRef + ".jpg")) 
                {
                    AddNewImage(_storeSettings.FolderImages.TrimEnd('/') + "/" + ProductRef + ".jpg", _storeSettings.FolderImagesMapPath.TrimEnd('\\') + "\\" + ProductRef + ".jpg");
                    upd = true;
                }
            }

            //Fix image paths
            var lp = 1;
            foreach (var i in Imgs)
            {
                if (!File.Exists(i.GetXmlProperty("genxml/hidden/imagepath"))) // products shared across portals may have different image path.
                {
                    if (!i.GetXmlProperty("genxml/hidden/imageurl").StartsWith(_storeSettings.FolderImages))
                    {
                        var iname = Path.GetFileName(i.GetXmlProperty("genxml/hidden/imagepath"));
                        DataRecord.SetXmlProperty("genxml/imgs/genxml[" + lp + "]/hidden/imageurl", _storeSettings.FolderImages.TrimEnd('/') + "/" + iname);
                        errorcount += 1;
                    }
                    if (!i.GetXmlProperty("genxml/hidden/imagepath").StartsWith(_storeSettings.FolderImagesMapPath))
                    {
                        var iname = Path.GetFileName(i.GetXmlProperty("genxml/hidden/imagepath"));
                        DataRecord.SetXmlProperty("genxml/imgs/genxml[" + lp + "]/hidden/imagepath", _storeSettings.FolderImagesMapPath.TrimEnd('\\') + "\\" + iname);
                        errorcount += 1;
                    }
                }
                lp += 1;
            }
            //Fix document paths
            lp = 1;
            foreach (var d in Docs)
            {
                if (!File.Exists(d.GetXmlProperty("genxml/hidden/filepath"))) // products shared across portals may have different path.
                {
                    if (!d.GetXmlProperty("genxml/hidden/filepath").StartsWith(_storeSettings.FolderDocumentsMapPath))
                    {
                        var fname = d.GetXmlProperty("genxml/textbox/txtfilename");
                        if (d.GetXmlProperty("genxml/hidden/filerelpath") != "")
                        {
                            fname = Path.GetFileName("D:" + d.GetXmlProperty("genxml/hidden/filerelpath").Replace("/", "\\"));
                        }
                        DataRecord.SetXmlProperty("genxml/docs/genxml/hidden/filepath", _storeSettings.FolderDocumentsMapPath.TrimEnd('\\') + "\\" + fname);
                        errorcount += 1;
                    }
                }
                lp += 1;
            }

            if (errorcount > 0) objCtrl.Update(DataRecord); // update if we find a error

            // fix langauge records
            foreach (var lang in DnnUtils.GetCultureCodeList(_portalId))
            {
                var l = objCtrl.GetList(_portalId, -1, _typeLangCode, " and NB1.ParentItemId = " + Info.ItemID.ToString("") + " and NB1.Lang = '" + lang + "'");
                if (l.Count == 0 && DataLangRecord != null)
                {
                    var nbi = (NBrightInfo)DataLangRecord.Clone();
                    nbi.ItemID = -1;
                    nbi.Lang = lang;
                    objCtrl.Update(nbi);
                    errorcount += 1;
                }
                if (l.Count > 1)
                {
                    // we have more records than should exists, remove any old ones.
                    var l2 = objCtrl.GetList(_portalId, -1, _typeLangCode, " and NB1.ParentItemId = " + Info.ItemID.ToString("") + " and NB1.Lang = '" + lang + "'", "order by Modifieddate desc");
                    var lp2 = 1;
                    foreach (var i in l2)
                    {
                        if (lp2 >= 2) objCtrl.Delete(i.ItemID);
                        errorcount += 1;
                        lp2 += 1;
                    }
                }
            }

            // remove duplicate category xrefs.
            var catlist = GetCategories();
            foreach (var c in catlist)
            {
                var l = objCtrl.GetList(_portalId, -1, "CATXREF", " and NB1.ParentItemId = " + Info.ItemID.ToString("") + " and NB1.XrefItemId = " + c.categoryid.ToString(""));
                if (l.Count > 1)
                {
                    var catlp = 0;
                    foreach (var i in l)
                    {
                        if (catlp >= 1)
                        {
                            objCtrl.Delete(i.ItemID);
                            errorcount += 1;
                        }
                        catlp += 1;
                    }
                }
            }

            // remove duplicate catcascade records
            var cascadeList = objCtrl.GetList(PortalSettings.Current.PortalId, -1, "CATCASCADE", " and NB1.ParentItemId = " + Info.ItemID.ToString("") );
            foreach (var c in cascadeList)
            {
                var l2 = objCtrl.GetList(PortalSettings.Current.PortalId, -1, "CATCASCADE", " and GUIDKey = '" + c.GUIDKey + "'");
                if (l2.Count > 1)
                {
                    for (int i = 1; i < l2.Count; i++)
                    {
                        objCtrl.Delete(l2[i].ItemID);
                        errorcount += 1;
                    }
                }
            }

            // remove any unlinked catacscade records
            cascadeList = objCtrl.GetList(PortalSettings.Current.PortalId, -1, "CATCASCADE", " and NB1.ParentItemId = " + Info.ItemID.ToString("") + " and NB1.XrefItemId = 0");
            foreach (var c in cascadeList)
            {
                objCtrl.Delete(c.ItemID);
                errorcount += 1;
            }


            // update shared product if flagged
            if (StoreSettings.Current.GetBool("shareproducts") && DataRecord.PortalId >= 0 ) 
            {
                upd = true;
                DataRecord.PortalId = -1;
                if (DataLangRecord != null)
                {
                    DataLangRecord.PortalId = -1;
                }
            }

            // check if we have empty model name ()
            var modellp = 1;
            foreach (var m in Models)
            {
                if (DataLangRecord != null && DataLangRecord.GetXmlProperty("genxml/models/genxml[" + modellp + "]/textbox/txtmodelname") == "")
                {
                    DataLangRecord.SetXmlProperty("genxml/models/genxml[" + modellp + "]/textbox/txtmodelname", ProductName);
                    upd = true;
                }
                if (DataRecord.GetXmlProperty("genxml/models/genxml[" + modellp + "]/textbox/txtmodelref") == "")
                {
                    DataRecord.SetXmlProperty("genxml/models/genxml[" + modellp + "]/textbox/txtmodelref", ProductRef);
                    upd = true;
                }
                // if sale or dealer disabled, set zero for price
                if (DataRecord.GetXmlProperty("genxml/models/genxml[" + modellp + "]/checkbox/chkdisablesale") == "True")
                {
                    if (DataRecord.GetXmlPropertyDouble("genxml/models/genxml[" + modellp + "]/textbox/txtsaleprice") > 0)
                    {
                        DataRecord.SetXmlProperty("genxml/models/genxml[" + modellp + "]/textbox/txtsaleprice", "0");
                        upd = true;
                    }
                }
                if (DataRecord.GetXmlProperty("genxml/models/genxml[" + modellp + "]/checkbox/chkdisabledealer") == "True")
                {
                    if (DataRecord.GetXmlPropertyDouble("genxml/models/genxml[" + modellp + "]/textbox/txtdealercost") > 0)
                    {
                        DataRecord.SetXmlProperty("genxml/models/genxml[" + modellp + "]/textbox/txtdealercost", "0");
                        upd = true;
                    }
                    if (DataRecord.GetXmlPropertyDouble("genxml/models/genxml[" + modellp + "]/textbox/txtdealersale") > 0)
                    {
                        DataRecord.SetXmlProperty("genxml/models/genxml[" + modellp + "]/textbox/txtdealersale", "0");
                        upd = true;
                    }
                }

                // if we have zero sale or dealer or delaersale, clear any promo desc
                if (DataRecord.GetXmlPropertyDouble("genxml/models/genxml[" + modellp + "]/textbox/txtdealersale") <= 0)
                {
                    DataRecord.RemoveXmlNode("genxml/models/genxml[" + modellp + "]/hidden/promodealersaleid");
                }
                if (DataRecord.GetXmlPropertyDouble("genxml/models/genxml[" + modellp + "]/textbox/txtsaleprice") <= 0)
                {
                    DataRecord.RemoveXmlNode("genxml/models/genxml[" + modellp + "]/hidden/promosalepriceid");
                }
                if (DataRecord.GetXmlPropertyDouble("genxml/models/genxml[" + modellp + "]/textbox/txtdealercost") <= 0)
                {
                    DataRecord.RemoveXmlNode("genxml/models/genxml[" + modellp + "]/hidden/promodealercostid");
                }

                if (DataRecord.GetXmlPropertyDouble("genxml/models/genxml[" + modellp + "]/textbox/txtdealersale") <= 0 && DataRecord.GetXmlPropertyDouble("genxml/models/genxml[" + modellp + "]/textbox/txtsaleprice") <= 0 && DataRecord.GetXmlPropertyDouble("genxml/models/genxml[" + modellp + "]/textbox/txtdealercost") <= 0)
                {
                    // promo removed, so clear and group promo data.
                    DataRecord.RemoveXmlNode("genxml/hidden/promotype");
                    DataRecord.RemoveXmlNode("genxml/hidden/promoname");
                    DataRecord.RemoveXmlNode("genxml/hidden/promoid");
                    DataRecord.RemoveXmlNode("genxml/hidden/promocalcdate");
                    DataRecord.RemoveXmlNode("genxml/hidden/datefrom");
                    DataRecord.RemoveXmlNode("genxml/hidden/dateuntil");
                }

                modellp += 1;
            }

            if (upd) // update if we've set the update flag.
            {
                objCtrl.Update(DataRecord);
                if (DataLangRecord != null)
                {
                    objCtrl.Update(DataLangRecord);
                }
            }


            return errorcount;
        }

        public int Copy()
        {

            var objCtrl = new NBrightBuyController();

            //Copy Base record 
            var dr = (NBrightInfo)DataRecord.Clone();
            dr.ItemID = -1;
            dr.SetXmlProperty("genxml/importref", Utils.GetUniqueKey());
            var newid = objCtrl.Update(dr);
            
            // copy all language records
            var l = objCtrl.GetList(_portalId, -1, _typeLangCode, " and NB1.ParentItemId = " + Info.ItemID.ToString(""));
            foreach (var dlr in l)
            {
                dlr.ParentItemId = newid;
                dlr.ItemID = -1;
                objCtrl.Update(dlr);
            }

            // copy CATXREF records
            l = objCtrl.GetList(_portalId, -1, "CATXREF", " and NB1.ParentItemId = " + Info.ItemID.ToString(""));
            foreach (var dr1 in l)
            {
                dr1.ParentItemId = newid;
                dr1.ItemID = -1;
                dr1.GUIDKey = dr1.GUIDKey.Replace("x" + Info.ItemID.ToString(""), "x" + newid.ToString(""));
                objCtrl.Update(dr1);
            }

            // copy CATCASCADE records
            l = objCtrl.GetList(_portalId, -1, "CATCASCADE", " and NB1.ParentItemId = " + Info.ItemID.ToString(""));
            foreach (var dr2 in l)
            {
                dr2.ParentItemId = newid;
                dr2.ItemID = -1;
                dr2.GUIDKey = dr2.GUIDKey.Replace("x" + Info.ItemID.ToString(""), "x" + newid.ToString(""));
                objCtrl.Update(dr2);
            }

            // copy PRDXREF records
            l = objCtrl.GetList(_portalId, -1, _typeCode + "XREF", " and NB1.ParentItemId = " + Info.ItemID.ToString(""));
            foreach (var dr3 in l)
            {
                dr3.ParentItemId = newid;
                dr3.ItemID = -1;
                dr3.GUIDKey = dr3.GUIDKey.Replace("x" + Info.ItemID.ToString(""), "x" + newid.ToString(""));
                objCtrl.Update(dr3);
                
                // create bi-directional relationship
                var newXrefId = dr3.ParentItemId; var newParentItemId = dr3.XrefItemId;
                dr3.ParentItemId = newParentItemId;
                dr3.XrefItemId = newXrefId;
                dr3.GUIDKey = newid.ToString("") + "x" + dr3.GUIDKey.Replace("x" + newid.ToString(""), "");
                objCtrl.Update(dr3);
            }            

            // copy USERPRDXREF records
            l = objCtrl.GetList(_portalId, -1, "USERPRDXREF", " and NB1.ParentItemId = " + Info.ItemID.ToString(""));
            foreach (var dr4 in l)
            {
                dr4.ParentItemId = newid;
                dr4.ItemID = -1;
                dr4.GUIDKey = dr4.GUIDKey.Replace("x" + Info.ItemID.ToString(""), "x" + newid.ToString(""));
                objCtrl.Update(dr4);
            }

            return newid;
        }

        public void FillEmptyLanguageFields()
        {
            var objCtrl = new NBrightBuyController();
            foreach (var toLang in DnnUtils.GetCultureCodeList(_portalId))
            {
                if (toLang != _lang)
                {
                    var dlang = objCtrl.GetDataLang(DataRecord.ItemID, toLang) ?? DataLangRecord;
                    dlang.Lang = toLang;
                    // product
                        var nodList = DataLangRecord.XMLDoc.SelectNodes("genxml/textbox/*");
                        if (nodList != null)
                        {
                            foreach (XmlNode nod in nodList)
                            {
                                if (nod.InnerText.Trim() != "")
                                {
                                    if (dlang.GetXmlProperty("genxml/textbox/" + nod.Name) == "")
                                    {
                                        dlang.SetXmlProperty("genxml/textbox/" + nod.Name, nod.InnerText);
                                    }
                                }
                            }
                        }

                        // editor
                        if (DataLangRecord.GetXmlProperty("genxml/edt/description") != "")
                        {
                            if (dlang.GetXmlProperty("genxml/edt/description") == "")
                            {
                                dlang.SetXmlProperty("genxml/edt/description", DataLangRecord.GetXmlPropertyRaw("genxml/edt/description"));
                            }
                        }


                        // models
                        var nodList1 = DataLangRecord.XMLDoc.SelectNodes("genxml/models/genxml");
                        if (nodList1 != null)
                        {
                            var c = 1;
                            foreach (XmlNode nod1 in nodList1)
                            {
                                nodList = nod1.SelectNodes("textbox/*");
                                if (nodList != null)
                                {
                                    foreach (XmlNode nod in nodList)
                                    {
                                        if (nod.InnerText.Trim() != "")
                                        {
                                            if (dlang.GetXmlProperty("genxml/models/genxml[" + c + "]/textbox/" + nod.Name) == "")
                                            {
                                                dlang.SetXmlProperty("genxml/models/genxml[" + c + "]/textbox/" + nod.Name, nod.InnerText);
                                            }
                                        }
                                    }
                                }
                                c += 1;
                            }
                        }

                        // options
                        nodList1 = DataLangRecord.XMLDoc.SelectNodes("genxml/options/genxml");
                        if (nodList1 != null)
                        {
                            var c = 1;
                            foreach (XmlNode nod1 in nodList1)
                            {
                                nodList = nod1.SelectNodes("textbox/*");
                                if (nodList != null)
                                {
                                    foreach (XmlNode nod in nodList)
                                    {
                                        if (nod.InnerText.Trim() != "")
                                        {
                                            if (dlang.GetXmlProperty("genxml/options/genxml[" + c + "]/textbox/" + nod.Name) == "")
                                            {
                                                dlang.SetXmlProperty("genxml/options/genxml[" + c + "]/textbox/" + nod.Name, nod.InnerText);
                                            }
                                        }
                                    }
                                }
                                c += 1;
                            }
                        }

                        // imgs
                        nodList1 = DataLangRecord.XMLDoc.SelectNodes("genxml/imgs/genxml");
                        if (nodList1 != null)
                        {
                            var c = 1;
                            foreach (XmlNode nod1 in nodList1)
                            {
                                nodList = nod1.SelectNodes("textbox/*");
                                if (nodList != null)
                                {
                                    foreach (XmlNode nod in nodList)
                                    {
                                        if (nod.InnerText.Trim() != "")
                                        {
                                            if (dlang.GetXmlProperty("genxml/imgs/genxml[" + c + "]/textbox/" + nod.Name) == "")
                                            {
                                                dlang.SetXmlProperty("genxml/imgs/genxml[" + c + "]/textbox/" + nod.Name, nod.InnerText);
                                            }
                                        }
                                    }
                                }
                                c += 1;
                            }
                        }

                        // docs
                        nodList1 = DataLangRecord.XMLDoc.SelectNodes("genxml/docs/genxml");
                        if (nodList1 != null)
                        {
                            var c = 1;
                            foreach (XmlNode nod1 in nodList1)
                            {
                                nodList = nod1.SelectNodes("textbox/*");
                                if (nodList != null)
                                {
                                    foreach (XmlNode nod in nodList)
                                    {
                                        if (nod.InnerText.Trim() != "")
                                        {
                                            if (dlang.GetXmlProperty("genxml/docs/genxml[" + c + "]/textbox/" + nod.Name) == "")
                                            {
                                                dlang.SetXmlProperty("genxml/docs/genxml[" + c + "]/textbox/" + nod.Name, nod.InnerText);
                                            }
                                        }
                                    }
                                }
                                c += 1;
                            }
                        }

                        // optionvalues
                        nodList1 = DataLangRecord.XMLDoc.SelectNodes("genxml/optionvalues/genxml");
                        if (nodList1 != null)
                        {
                            var c = 1;
                            foreach (XmlNode nod1 in nodList1)
                            {
                                nodList = nod1.SelectNodes("textbox/*");
                                if (nodList != null)
                                {
                                    foreach (XmlNode nod in nodList)
                                    {
                                        if (nod.InnerText.Trim() != "")
                                        {
                                            if (dlang.GetXmlProperty("genxml/optionvalues/genxml[" + c + "]/textbox/" + nod.Name) == "")
                                            {
                                                dlang.SetXmlProperty("genxml/optionvalues/genxml[" + c + "]/textbox/" + nod.Name, nod.InnerText);
                                            }
                                        }
                                    }
                                }
                                c += 1;
                            }
                        }                        

                    objCtrl.Update(dlang);
                }
            }
        }


        public void OutputDebugFile(String filePathName)
        {
            Info.XMLDoc.Save(filePathName);
        }




        #endregion

        #region " private functions"

        private void SetGuidKey()
        {
            if (DataRecord.GetXmlProperty("genxml/importref") == "") DataRecord.SetXmlProperty("genxml/importref", Utils.GetUniqueKey(10).ToLower());

            if (_storeSettings.GetBool(StoreSettingKeys.friendlyurlids))
            {
                var refcode = DataRecord.GetXmlProperty("genxml/textbox/txtproductref").Trim();
                if (refcode != "")
                    DataRecord.GUIDKey = GetUniqueGuidKey(DataRecord.ItemID, Utils.UrlFriendly(refcode)).ToLower();
                else
                    DataRecord.GUIDKey = DataRecord.GetXmlProperty("genxml/importref");
            }
            else
            {
                DataRecord.GUIDKey = DataRecord.GetXmlProperty("genxml/importref");
            }
        }

        private string GetUniqueGuidKey(int productId, string newGUIDKey)
        {
            // make sure we have a unique guidkey
            var objCtrl = new NBrightBuyController();
            var doloop = true;
            var lp = 1;
            var testGUIDKey = newGUIDKey.ToLower();
            while (doloop)
            {
                var obj = objCtrl.GetByGuidKey(_portalId, -1, _typeCode, testGUIDKey);
                if (obj != null && obj.ItemID != productId)
                {
                    testGUIDKey = newGUIDKey + lp;
                }
                else
                    doloop = false;

                lp += 1;
                if (lp > 999) doloop = false; // make sure we never get a infinate loop
            }
            return testGUIDKey;
        }


        private void LoadData(int productId, Boolean hydrateLists = true)
        {
            Exists = false;
            var objCtrl = new NBrightBuyController();
            if (productId == -1) productId = AddNew(); // add new record if -1 is used as id.
            Info = objCtrl.Get(productId, _typeLangCode, _lang);
            if (Info != null)
            {
                if(_portalId<0) _portalId = PortalSettings.Current.PortalId;
                _storeSettings = new StoreSettings(_portalId);
                Exists = true;
                if (hydrateLists)
                {
                    //build model list
                    Models = GetEntityList("models");
                    Options = GetEntityList("options");
                    Imgs = GetEntityList("imgs");
                    Docs = GetEntityList("docs");

                    OptionValues = new List<NBrightInfo>();
                    foreach (var o in Options)
                    {
                        var l = GetOptionValuesById(o.GetXmlProperty("genxml/hidden/optionid"));
                        OptionValues.AddRange(l);   
                    }
                }
                Exists = true;
                DataRecord = objCtrl.GetData(productId);
                _typeCode = DataRecord.TypeCode;
                DataLangRecord = objCtrl.GetDataLang(productId, _lang);
                if (DataLangRecord == null) // rebuild langauge is we have a missing lang record
                {
                    Validate();
                    DataLangRecord = objCtrl.GetDataLang(productId, _lang);
                }
                _typeLangCode = DataLangRecord.TypeCode;

                IsOnSale = CheckIsOnSale();
                DealerIsOnSale = DealerCheckIsOnSale();
                IsInStock = CheckIsInStock();
                ClientFileUpload = CheckClientFileUpload(); 
            }
            else
            {
                Exists = false;
            }
        }

        private int AddNew()
        {

            var nbi = new NBrightInfo(true);
            if (StoreSettings.Current.Get("shareproducts") == "True") // option in storesetting to share products created here across all portals.
                _portalId = -1;
            else
                _portalId = PortalSettings.Current.PortalId;
            nbi.PortalId = _portalId;
            nbi.TypeCode = _typeCode;
            nbi.ModuleId = -1;
            nbi.ItemID = -1;
            nbi.SetXmlProperty("genxml/checkbox/chkishidden", "True");
            nbi.AddSingleNode("models", "<genxml><hidden><modelid>" + Utils.GetUniqueKey() + "</modelid></hidden></genxml>", "genxml");
            var objCtrl = new NBrightBuyController();
            var itemId = objCtrl.Update(nbi);

            foreach (var lang in DnnUtils.GetCultureCodeList(_portalId))
            {
                nbi = new NBrightInfo(true);
                nbi.PortalId = _portalId;
                nbi.TypeCode = _typeLangCode;
                nbi.ModuleId = -1;
                nbi.ItemID = -1;
                nbi.Lang = lang;
                nbi.ParentItemId = itemId;
                objCtrl.Update(nbi);
            }

            return itemId;
        }

        private List<NBrightInfo> GetEntityList(String entityName)
        {
            var l = new List<NBrightInfo>();
            if (Info != null && Info.XMLDoc != null)
            {
                var xmlNodList = Info.XMLDoc.SelectNodes("genxml/" + entityName + "/*");
                // build generic list to bind to rpModelsLang List
                if (xmlNodList != null && xmlNodList.Count > 0)
                {
                    var lp = 1;
                    foreach (XmlNode xNod in xmlNodList)
                    {
                        var obj = new NBrightInfo();
                        obj.XMLData = xNod.OuterXml;
                        obj.ItemID = lp;
                        obj.Lang = Info.Lang;                        
                        var nodLang = "<genxml>" + Info.GetXmlNode("genxml/lang/genxml/" + entityName + "/genxml[" + lp.ToString("") + "]") + "</genxml>";
                        if (nodLang != "")
                        {
                            obj.AddSingleNode("lang", "", "genxml");
                            obj.AddXmlNode(nodLang, "genxml", "genxml/lang");
                        }
                        obj.ParentItemId = Info.ItemID;
                        l.Add(obj);
                        lp += 1;
                    }
                }
            }
            return l;
        }

        private Boolean CheckIsInStock()
        {
            foreach (var obj in Models)
            {
                if (IsModelInStock(obj)) return true;
            }
            return false;
        }

        public Boolean IsModelInStock(NBrightInfo dataItem)
        {
            var stockOn = dataItem.GetXmlPropertyBool("genxml/checkbox/chkstockon");
            if (stockOn)
            {
                var modelstatus = dataItem.GetXmlProperty("genxml/dropdownlist/modelstatus");
                if (modelstatus == "010") return true;
            }
            else
            {
                return true;
            }
            return false;
        }

        private Boolean CheckIsOnSale()
        {
            var saleprice = GetSalePriceDouble();
            if (saleprice > 0) return true;
            return false;
        }

        public Double GetSalePriceDouble()
        {
            Double price = -1;
            foreach (var m in Models)
            {
                var s = m.GetXmlPropertyDouble("genxml/textbox/txtsaleprice");
                if ((s > 0) && (s < price) | (price == -1)) price = s;
            }
            if (price == -1) price = 0;
            return price;
        }

        private Boolean DealerCheckIsOnSale()
        {
            var saleprice = GetDealerSalePriceDouble();
            if (saleprice > 0) return true;
            return false;
        }

        public Double GetDealerSalePriceDouble()
        {
            Double price = -1;
            foreach (var m in Models)
            {
                var s = m.GetXmlPropertyDouble("genxml/textbox/txtdealersale");
                if ((s > 0) && (s < price) | (price == -1)) price = s;
            }
            if (price == -1) price = 0;
            return price;
        }


        private bool CheckClientFileUpload()
        {
            return Info.GetXmlPropertyBool("genxml/checkbox/chkfileupload");
        }

        #endregion
    }
}
