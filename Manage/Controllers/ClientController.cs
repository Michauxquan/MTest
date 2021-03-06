﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

using IntFactoryBusiness;
using IntFactoryBusiness.Manage;
using CloudSalesTool;
using IntFactoryEntity.Manage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IntFactoryEnum;
namespace YXManage.Controllers
{
    [YXManage.Common.UserAuthorize]
    public class ClientController : BaseController
    {
        //
        // GET: /Client/
        #region Client view
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Create()
        {
            ViewBag.Industry = IndustryBusiness.GetIndustrys();
            return View();
        } 
        public ActionResult Detail(string id)
        {
            ViewBag.ID = id;
            ViewBag.Industry = IndustryBusiness.GetIndustrys();
            return View();
        } 
        public ActionResult OrderDetail(string id)
        {
            ViewBag.ID = id; 
            return View();
        } 
        public ActionResult Orders()
        {
            return View("Orders");
        }
        #endregion

        #region Ajax

        /// <summary>
        /// 获取客户端列表
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="keyWords"></param>
        /// <returns></returns>
        public JsonResult GetClients(int pageIndex, string keyWords, int type = -1, string orderBy = " AutoID ")
        {
            int totalCount = 0, pageCount = 0;
            var list = ClientBusiness.GetClients(keyWords.Trim(), (EnumRegisterType)type,orderBy, PageSize, pageIndex, ref totalCount, ref pageCount);
            JsonDictionary.Add("Items", list);
            JsonDictionary.Add("TotalCount", totalCount);
            JsonDictionary.Add("PageCount", pageCount);
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        public JsonResult GetClientDetail(string id)
        {
            var item = ClientBusiness.GetClientDetail(id);
            JsonDictionary.Add("Item", item);
            JsonDictionary.Add("Result", 1);
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        public JsonResult GetClientAuthorizeLogs(string clientID,int pageIndex, string keyWords)
        {
            int totalCount = 0, pageCount = 0;
            var list = ClientBusiness.GetClientAuthorizeLogs(clientID,keyWords.Trim(), PageSize, pageIndex, ref totalCount, ref pageCount);
            JsonDictionary.Add("Items", list);
            JsonDictionary.Add("TotalCount", totalCount);
            JsonDictionary.Add("PageCount", pageCount);
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }
        /// <summary>
        /// 添加行业
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public JsonResult CreateIndustry(string name)
        {
            string id =IndustryBusiness.InsertIndustry(name, "", string.Empty, ClientID);
            JsonDictionary.Add("ID", id);
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        /// <summary>
        /// 添加客户端
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns> 
        public JsonResult SaveClient(string client, string loginName)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Clients model = serializer.Deserialize<Clients>(client);
            int result;
            string userid = "";
            if (string.IsNullOrEmpty(model.ClientID))
            {
                string clientid = ClientBusiness.InsertClient(EnumRegisterType.Manage, EnumAccountType.UserName, loginName, loginName, model.CompanyName, model.ContactName, model.MobilePhone, "", "", 
                                                              model.CityCode, model.Address, model.Description, "", CurrentUser.UserID, out result, out userid);
                JsonDictionary.Add("Result", result);
                JsonDictionary.Add("ClientID", clientid);
            }
            else
            {
                bool flag = ClientBusiness.UpdateClient(model, CurrentUser.UserID);

                JsonDictionary.Add("Result", flag?1:0);
            }

            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        } 

        public JsonResult DeleteClient(string id)
        {
            bool flag = ClientBusiness.DeleteClient(id);
            JsonDictionary.Add("Result", flag?1:0);
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        /// <summary>
        /// 账号是否存在
        /// </summary>
        /// <param name="loginName"></param>
        /// <returns></returns>
        public JsonResult IsExistLoginName(string loginName)
        {
            bool bl = OrganizationBusiness.IsExistLoginName(loginName);
            JsonDictionary.Add("Result", bl);
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }


        /// <summary>
        /// 升级客户服务
        /// </summary>
        /// <param name="client"></param> 
        public JsonResult SaveClientAuthorize(string clientID, int serviceType, int giveType, int userQuantity, string endTime, int buyType, int buyUserQuantity, int buyUserYears)
        {
            bool flag = false;
            var client = ClientBusiness.GetClientDetail(clientID);
            ClientAuthorizeLog log = new ClientAuthorizeLog();

            log.CreateUserID = CurrentUser.UserID;
            log.ClientID = clientID;
            log.OrderID = string.Empty;
            
            if (serviceType == 1)//赠送
            {
                if (giveType == 1)//赠送人数
                {
                    flag = ClientBusiness.AddClientUserQuantity(client.ClientID, userQuantity);

                    log.BeginTime = client.EndTime;
                    log.EndTime = client.EndTime;
                    log.UserQuantity = userQuantity;
                    log.Type = 2;
                }
                else//赠送时间
                {
                    log.BeginTime = client.EndTime;
                    flag = ClientBusiness.SetClientEndTime(client.ClientID, DateTime.Parse(endTime));

                    log.EndTime = DateTime.Parse(endTime);
                    log.UserQuantity = client.UserQuantity;
                    log.Type = 3;
                }
                ClientBusiness.UpdateClientCache(client.ClientID);
                ClientBusiness.InsertClientAuthorizeLog(log);
                ClearClientCache(client.ClientID);
            }
            else//购买生成订单
            {
                log.Type = buyType;
                int remainderMonths = 0;//剩余月份
                int years = 1;

                if (buyType == 2)//购买人数
                {
                    remainderMonths = (client.EndTime.Year - DateTime.Now.Year) * 12 + (client.EndTime.Month - DateTime.Now.Month) - 1;
                    if (client.EndTime.Day >= DateTime.Now.Day)
                        remainderMonths += 1;

                    years = remainderMonths / 12 == 0 ? 1 : remainderMonths / 12;

                    log.BeginTime = client.EndTime;
                    log.EndTime = client.EndTime;
                    log.UserQuantity = userQuantity;
                }
                else
                {
                    years = buyUserYears;

                    log.BeginTime = client.EndTime;
                    log.EndTime = client.EndTime.AddYears(years);
                    log.UserQuantity = client.UserQuantity;
                }

                int pageCount = 0;
                int totalCount = 0;
                List<ModulesProduct> list = ModulesProductBusiness.GetModulesProducts(string.Empty, int.MaxValue, 1, ref totalCount, ref pageCount);

                //获取订单产品的最佳组合
                var way = ModulesProductBusiness.GetBestWay(buyUserQuantity, list.OrderByDescending(m => m.UserQuantity).Where(m => m.PeriodQuantity == years).ToList());

                //获取订单参数
                ClientOrder model = new ClientOrder();
                model.UserQuantity = way.TotalQuantity;
                model.Type = buyType;
                model.Years = years;
                model.Amount = way.TotalMoney;
                model.RealAmount = way.TotalMoney;
                model.SourceType = 1;
                //购买人数
                float remainderYears = 1;
                if (buyType == 2)
                {
                    remainderYears = (float)remainderMonths / (12 * years);
                    model.Amount = decimal.Parse((float.Parse(model.Amount.ToString()) * remainderYears).ToString("f2"));
                    model.RealAmount = model.Amount;
                }
                model.ClientID = client.ClientID;
                model.CreateUserID = CurrentUser.UserID;

                model.Details = new List<ClientOrderDetail>();
                foreach (var p in way.Products)
                {
                    ClientOrderDetail detail = new ClientOrderDetail();
                    detail.ProductID = p.Key;
                    detail.Qunatity = p.Value;
                    detail.CreateUserID = CurrentUser.CreateUserID;
                    detail.Price = list.Find(m => m.ProductID == p.Key).Price;
                    //购买人数
                    if (buyType == 2)
                    {
                        detail.Price = decimal.Parse((float.Parse(detail.Price.ToString()) * remainderYears).ToString("f2"));
                    }
                    model.Details.Add(detail);
                }

                string orderID = ClientOrderBusiness.AddClientOrder(model);
                log.OrderID = orderID;

                flag = string.IsNullOrEmpty(orderID) ? false : true;
            }

            JsonDictionary.Add("Result", flag ? 1 : 0);

            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }
        /// <summary>
        /// 根据订单clientID获取订单
        /// </summary> 
        public JsonResult GetClientOrders(string keyWords, string clientID, int status, int type, string beginDate, string endDate, int pageSize, int pageIndex, int userType=0)
        {
            int pageCount = 0;
            int totalCount = 0;

            List<ClientOrder> list = ClientOrderBusiness.GetBase(keyWords.Trim(), status, type, beginDate, endDate, clientID, userType, pageSize, pageIndex, ref totalCount, ref pageCount);
            JsonDictionary.Add("Items", list);
            JsonDictionary.Add("TotalCount", totalCount);
            JsonDictionary.Add("PageCount", pageCount);

            return new JsonResult
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }
        /// <summary>
        /// 关闭客户订单
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns> 
        public JsonResult CloseClientOrder(string id)
        {
             ClientOrder order=ClientOrderBusiness.GetClientOrderInfo(id);
            if (order.Status == 0)
            {
                bool flag = ClientOrderBusiness.UpdateClientOrderStatus(id, IntFactoryEnum.EnumClientOrderStatus.Delete);
                JsonDictionary.Add("Result", flag ? 1 : 0);
            }
            else
            {
                JsonDictionary.Add("Result", order.Status == 1 ? 1001 : 1002);
            }
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        /// <summary>
        /// 修改客户订单支付金额
        /// </summary>
        /// <param name="id"></param>
        /// <param name="amount"></param>
        /// <returns></returns> 
        public JsonResult UpdateOrderAmount(string id,string amount)
        {
            ClientOrder order=ClientOrderBusiness.GetClientOrderInfo(id);
            if (order.Status == 0)
            {
                bool flag = ClientOrderBusiness.UpdateOrderAmount(id, decimal.Parse(amount));
                JsonDictionary.Add("Result", flag ? 1 : 0);
            }
            else
            {
                JsonDictionary.Add("Result", order.Status == 1 ? 1001 : 1002);
            }
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        /// <summary>
        /// 审核通过客户订单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="agentID"></param>
        /// <returns></returns> 
        public JsonResult PayOrderAndAuthorizeClient(string id)
        {
            //订单支付及后台客户授权
            ClientOrder order = ClientOrderBusiness.GetClientOrderInfo(id);
            if (order.Status == 0)
            {
                bool flag = ClientOrderBusiness.PayOrderAndAuthorizeClient(id, CurrentUser.UserID, -1, EnumOrderPayType.Cash);
                JsonDictionary.Add("Result", flag ? 1 : 0);

                if (flag)
                {
                    ClientBusiness.UpdateClientCache(order.ClientID);
                    ClearClientCache(order.ClientID);
                }
            }else {
                JsonDictionary.Add("Result", order .Status== 1 ? 1001 : 1002);
            }
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet

            };
        } 

        public JsonResult PayClientOrder(string id)
        {
            
            ClientOrder order = ClientOrderBusiness.GetClientOrderInfo(id);
            if (order.PayStatus == 0 || order.PayStatus == 2)
            {
                bool flag = ClientOrderBusiness.PayClientOrder(id, (int)IntFactoryEnum.EnumClientOrderPay.Pay);
                JsonDictionary.Add("Result", flag ? 1 : 0);
            }
            else {
                JsonDictionary.Add("Result", 1001);
            }
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        public JsonResult GetClientOrderDetail(string orderid) 
        {
            var item = ClientOrderBusiness.GetClientOrderInfo(orderid);
            JsonDictionary.Add("Item", item);
            JsonDictionary.Add("Result", 1);
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        public JsonResult GetClientOrderAccount(string keyWords, string orderID, string clientID, int payType,int status, int type, int pageSize, int pageIndex)
        {
            int totalCount = 0, pageCount = 0;
            var list = ClientOrderAccountBusiness.GetClientOrderAccounts(keyWords.Trim(), orderID, clientID,payType,status,type, PageSize, pageIndex, ref totalCount, ref pageCount);
            JsonDictionary.Add("Items", list);
            JsonDictionary.Add("TotalCount", totalCount);
            JsonDictionary.Add("PageCount", pageCount);
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }
        
        public JsonResult CheckOrderAccount(string id) 
        { 
            int flag = ClientOrderAccountBusiness.UpdateClientOrderAccountStatus(id, (int)IntFactoryEnum.EnumClientOrderStatus.Pay);
            JsonDictionary.Add("Result", flag  );
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        public JsonResult CloseOrderAccount(string id)
        {
            int flag = ClientOrderAccountBusiness.UpdateClientOrderAccountStatus(id, (int)IntFactoryEnum.EnumClientOrderStatus.Delete);
            JsonDictionary.Add("Result", flag);
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        } 

        public JsonResult AddOrderAccount(string orderAccount)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            ClientOrderAccount model = serializer.Deserialize<ClientOrderAccount>(orderAccount);
            int result=0;
            model.CreateUserID = CurrentUser.UserID;
            result = ClientOrderAccountBusiness.AddClientOrderAccount(model);
            JsonDictionary.Add("Result", result);
            return new JsonResult()
            {
                Data = JsonDictionary,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        #endregion

    }
}
