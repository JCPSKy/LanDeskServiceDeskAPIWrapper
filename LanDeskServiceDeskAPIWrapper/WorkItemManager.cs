using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace LanDeskServiceDeskAPIWrapper
{
    public class WorkItemManager
    {
        private readonly Uri _baseAddress;
        private readonly CookieContainer _cookieContainer;
        private readonly string _username;
        private readonly string _password;
        
        public WorkItemManager(string baseAddress, string username, string password)
        {
            _username = username;
            _password = password;
            _baseAddress = new Uri(baseAddress);
            _cookieContainer = new CookieContainer();
            ToLanDesk(LanDeskLogin, new LanDeskItem());
        }

        private LanDeskItem LanDeskLogin(HttpClient client, LanDeskItem login)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Ecom_User_ID", _username),
                new KeyValuePair<string, string>("Ecom_User_Password",  _password)
            });
            var post = client.PostAsync("wd/Logon/Logon.rails", content);
            var postResult = post.Result;
            postResult.EnsureSuccessStatusCode();
            if (
                postResult.Headers.GetValues("X-RequestUrl")
                    .Single()
                    .Contains("Logon/IntegratedLogonFailed.rails"))
            {
                login.Success = false;
            }
            login.Success = true;
            return login;
        }

        public T ToLanDesk<T, T1>(Func<HttpClient, T1, T> action, T1 item)
        {
            try
            {
                using (var handler = new HttpClientHandler { CookieContainer = _cookieContainer })
                {
                    handler.UseDefaultCredentials = false;
                    using (var client = new HttpClient(handler))
                    {
                        client.BaseAddress = _baseAddress;
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Add("User-Agent",
                            "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

                        return action(client, item);
                    }
                }
            }
            catch (Exception exception)
            {
                throw new Exception("HttpRequest to LanDesk Failed.", exception);
            }
        }

        public bool SendClosure(LanDeskItem ldi)
        {
            if (ldi.attributes.Status == Constants.ClosedState
                || ldi.attributes.Status == Constants.CancelledState
                || ldi.attributes.Status == Constants.RejectedState
                || ldi.attributes.Status == Constants.ResolvedState
                || ldi.attributes.Status == Constants.AwaitingServiceDeskReviewState
                || ldi.attributes.Status == "With Customer")
            {
                return true;
            }
            try
            {
                return !string.IsNullOrEmpty(ClosureSteps(ldi).value);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private LanDeskItem ClosureSteps(LanDeskItem ldi)
        {
            var taskClosure = ToLanDesk(GetAndCloseChildTasks, ldi);
            if (!taskClosure.Success)
            {
               throw new Exception("Failed to properly close all of the child tasks during the closure process.");
            }
            var parentClass = ldi.className;
            string function;
            string message;
            switch (parentClass)
            {
                case LanDeskItemType.Request:
                    function = "AddProvisionConfirmation";
                    message =
                        "Development on this Request was completed. No additional code or modifications are necessary at this time. The code, if necessary, has been built to production web servers and should be available for the user to use at this time.";
                    break;
                case LanDeskItemType.Incident:
                    function = "Resolve";
                    message =
                        "The bug, error or omission has been corrected. The user should no longer experience the specific concern addressed in this Incident.";
                    break;
                case LanDeskItemType.Problem:
                    function = "AddDiagnosis";
                    message =
                        "Developers have determined that issue could be resolved by taking steps to modify, alter or otherwise impact the system affected.";
                    break;
                default:
                    return new LanDeskItem { Success = false };
            }
            ExecuteFunction(ldi, function, message);
            ldi.Success = true;
            if (parentClass != LanDeskItemType.Problem) return ldi;
            ldi.Success = false;

            function = "AddSystemCause";
            message =
                "The system which underlies this Problem contains one or more correctable errors. These errors are systemic and may be altered either on the server or via manipulation of the code controlling this system.";
            ExecuteFunction(ldi, function, message);

            function = "Resolve";
            message =
                "The issue or concern at the heart of this Problem has been corrected. As a consequence, all users should be able to continue to use the system without continuing to experience the same error.";
            ExecuteFunction(ldi, function, message);

            ldi.Success = true;
            return ldi;
        }

        private LanDeskItem GetAndCloseChildTasks(HttpClient client, LanDeskItem lanDeskItem)
        {
            var parentClass = lanDeskItem.className;
            var parentKey = lanDeskItem.value;
            dynamic taskResult;
            switch (parentClass)
            {
                case LanDeskItemType.Request:
                    taskResult =
                        client.GetAsync(
                            @"wd/query/list.rails?class_name=RequestManagement.Task&cns=Request-c-0&c0=" +
                            parentKey + "&attributes=Status%2CCreationDate%2CCreationUser").Result;
                    break;
                case LanDeskItemType.Incident:
                    taskResult =
                        client.GetAsync(
                            @"wd/query/list.rails?class_name=IncidentManagement.Task&cns=Incident-c-0&c0=" +
                            parentKey + "&attributes=Status%2CCreationDate%2CCreationUser").Result;
                    break;
                case LanDeskItemType.Problem:
                    taskResult =
                        client.GetAsync(
                            @"wd/query/list.rails?class_name=ProblemManagement.Task&cns=Problem-c-0&c0=" +
                            parentKey + "&attributes=Status%2CCreationDate%2CCreationUser").Result;
                    break;
                default:
                    return new LanDeskItem { Success = false };
            }
            var taskContent =
                JsonConvert.DeserializeObject<QueryResult>(taskResult.Content.ReadAsStringAsync().Result);
            foreach (var task in taskContent.objects)
            {
                if (task.attributes.Status == "Completed") continue;
                ExecuteFunction((LanDeskItem)task, "Complete", "This task has been completed by the System Development department.");
            }
            lanDeskItem.Success = true;
            return lanDeskItem;
        }

        public LanDeskItem GetLanDeskItemByTypeAndGuid(string type, string guid)
        {
            return ToLanDesk(OpenLanDeskItemSteps, new LanDeskItem { className = type, value = guid });
        }
        public LanDeskItem OpenLanDeskItemSteps(HttpClient client, LanDeskItem lanDeskItem)
        {
            var itemPartUrl = $"wd/object/open.rails?class_name={lanDeskItem.className}&key={lanDeskItem.value}";

            var itemGet = client.GetAsync(itemPartUrl).Result;
            if (!itemGet.IsSuccessStatusCode)
            {
                throw new Exception("No LanDesk items matched this query.");
            }

            return JsonConvert.DeserializeObject<LanDeskItem>(itemGet.Content.ReadAsStringAsync().Result);
        }

        public LanDeskItem GetLanDeskItemByTypeAndId(string type, string id)
        {
            var ldt = new LanDeskItemType(type);
            var queryBuilder = new QueryBuilder(ldt);
            var conditions = new Condition { Operator = Operator.Equals, Property = "Id", Variable = id };
            var query = queryBuilder.CreateQuery(50, "Guid", conditions, LanDeskItemType.GetProperties(ldt.Name).Take(30).ToArray());
            var res = ToLanDesk(ExecuteQuery, query).FirstOrDefault();
            if (res == null)
            {
                return null;
            }
            var queryItem = ToLanDesk(OpenLanDeskItemSteps, res);
            if (queryItem == null)
            {
                return null;
            }
            queryItem.attributes.Class = res.attributes.Class;
            queryItem.attributes.ConfigurationItemType = res.attributes.ConfigurationItemType;
            queryItem.attributes.CreateGroup = res.attributes.CreateGroup;
            queryItem.attributes.CreationUser = res.attributes.CreationUser;
            queryItem.attributes.CurrentAssignment = res.attributes.CurrentAssignment;
            queryItem.attributes.LastUpdateUser = res.attributes.LastUpdateUser;
            queryItem.attributes.LatestAssignmentGroup = res.attributes.LatestAssignmentGroup;
            queryItem.attributes.Lifecycle = res.attributes.Lifecycle;
            queryItem.attributes.RaiseUser = res.attributes.RaiseUser;
            queryItem.attributes.Status = res.attributes.Status;
            queryItem.attributes.UpdateGroup = res.attributes.UpdateGroup;
            queryItem.attributes._CatalogueHierarchy = res.attributes._CatalogueHierarchy;
            return queryItem;
        }

        public LanDeskItem ClassAndIdGetSteps(HttpClient client, LanDeskItem lanDeskItem)
        {
            var queryPartUrl =
                string.Format($"wd/query/list.rails?class_name={lanDeskItem.className}&cns=Id-c-0-&c0={lanDeskItem.name}");

            var queryGet = client.GetAsync(queryPartUrl).Result;
            if (!queryGet.IsSuccessStatusCode)
            {
                throw new Exception("No LanDesk items matched this query.");
            }

            return JsonConvert.DeserializeObject<QueryResult>(queryGet.Content.ReadAsStringAsync().Result).objects.FirstOrDefault();
        }

        public LanDeskItem AddPublicNote(LanDeskItem ldi, string message)
        {
            ldi.Success = false;
            ExecuteFunction(ldi, "AddNote", message);
            ldi.Success = true;
            return ldi;
        }

        public LanDeskItem AddPrivateNote(LanDeskItem ldi, string message)
        {
            ldi.Success = false;
            ExecuteFunction(ldi, "AddPrivateNote", message);
            ldi.Success = true;
            return ldi;
        }

        // TODO: Fix missing form elements below for item creation.
        /// <summary>
        /// Creates and Saves a new LanDesk Item.
        /// </summary>
        /// <param name="itemType"> For example, "RequestManagement.Request"</param>
        /// <param name="title"></param>
        /// <param name="details"></param>
        /// <returns>The new LanDesk item.</returns>
        public LanDeskItem CreateLanDeskItem(string itemType, string title, string details)
        {
            var newObject = ToLanDesk(CreateSteps, new LanDeskItem { className = itemType });
            newObject.attributes.Title = title;
            newObject.attributes.Description = details;
            var formContent = GenerateForm(newObject, "Create " + newObject.className, "", null);
            return ToLanDesk(SaveSteps, formContent);
        }

        public LanDeskItem CreateSteps(HttpClient client, LanDeskItem lanDeskItem)
        {
            var queryPartUrl = string.Format("wd/object/create.rails?class_name={0}Management.{0}", lanDeskItem.className);
            var objectGet = client.GetAsync(queryPartUrl).Result;
            if (!objectGet.IsSuccessStatusCode)
            {
                throw new Exception("LanDesk prevented creation of a new item of this type.");
            }
            var newObject = JsonConvert.DeserializeObject<LanDeskItem>(objectGet.Content.ReadAsStringAsync().Result);
            return newObject;
        }

        public void ExecuteFunction(LanDeskItem lanDeskItem, string function, string message)
        {
            lanDeskItem.Function = function;
            var functionObject = ToLanDesk(FunctionSteps, lanDeskItem);
            var formContent = GenerateForm(lanDeskItem, function, message, functionObject);
            var result = ToLanDesk(SaveSteps, formContent);
            if (result != null && result.Success) return;
            throw new Exception("Function " + function + " failed not execute properly.");
        }

        private LanDeskItem FunctionSteps(HttpClient client, LanDeskItem lanDeskItem)
        {
            var functionResult =
                (dynamic)
                    client.GetAsync(@"wd/object/invokeFunction.rails?class_name=" + lanDeskItem.className
                                        + "&key=" + lanDeskItem.value +
                                        "&function_name=" + lanDeskItem.Function).Result;
            return JsonConvert.DeserializeObject<LanDeskItem>(functionResult.Content.ReadAsStringAsync().Result);
        }

        private LanDeskItem SaveSteps(HttpClient client, FormUrlEncodedContent formContent)
        {
            var saveResult = (dynamic)client.PostAsync("Object/Save.rails", formContent).Result;
            if (!saveResult.IsSuccessStatusCode)
            {
                return new LanDeskItem { Success = false };
            }
            var resultObject = JsonConvert.DeserializeObject<LanDeskItem>(saveResult.Content.ReadAsStringAsync().Result);
            resultObject.Success = true;
            return resultObject;
        }

        public IEnumerable<LanDeskItem> GetLanDeskItemsOfTypesForAssignedGroupsAndLatestUpdate(string[] types, string[] teams, int minutesPassed)
        {
            foreach (var typeString in types)
            {
                var type = new LanDeskItemType(typeString);
                foreach (var team in teams)
                {
                    const string format = "MM/dd/yyyy hh:mm:ss tt";
                    var strDate = DateTime.Now.AddMinutes(minutesPassed).ToString(format);
                    CultureInfo en = new CultureInfo("en-US");
                    var parsedBack = DateTime.ParseExact(strDate, format, en.DateTimeFormat);
                    var dateParsed = parsedBack.ToString();
                    var lanDeskItem = new LanDeskItem { Type = type, attributes = { _CurrentAssignedGroup = team, LastUpdate = dateParsed } };
                    var cn = new BooleanCondition
                    {
                        Condition1 = new Condition

                        {
                            Operator = Operator.Equals,
                            Property = "_CurrentAssignedGroup",
                            Variable = lanDeskItem.attributes._CurrentAssignedGroup
                        },
                        ConditionType = ConditionType.And,
                        Condition2 = new Condition
                        {
                            Operator = Operator.GreaterThan,
                            Property = "LastUpdate",
                            Variable = lanDeskItem.attributes.LastUpdate
                        }
                    };
                    var query = new QueryBuilder(lanDeskItem.Type);
                    var fullQuery = query.CreateQuery(0, lanDeskItem.Type.AvailableProperties[0], cn, lanDeskItem.Type.AvailableProperties.Take(30).ToArray());
                    var queryResults = ToLanDesk(ExecuteQuery, fullQuery);
                    if (queryResults == null) continue;
                    foreach (var res in queryResults)
                    {
                        var queryItem = GetLanDeskItemByTypeAndGuid(res.className, res.value);
                        queryItem.attributes.Class = res.attributes.Class;
                        queryItem.attributes.ConfigurationItemType = res.attributes.ConfigurationItemType;
                        queryItem.attributes.CreateGroup = res.attributes.CreateGroup;
                        queryItem.attributes.CreationUser = res.attributes.CreationUser;
                        queryItem.attributes.CurrentAssignment = res.attributes.CurrentAssignment;
                        queryItem.attributes.LastUpdateUser = res.attributes.LastUpdateUser;
                        queryItem.attributes.LatestAssignmentGroup = res.attributes.LatestAssignmentGroup;
                        queryItem.attributes.Lifecycle = res.attributes.Lifecycle;
                        queryItem.attributes.RaiseUser = res.attributes.RaiseUser;
                        queryItem.attributes.Status = res.attributes.Status;
                        queryItem.attributes.UpdateGroup = res.attributes.UpdateGroup;
                        queryItem.attributes._CatalogueHierarchy = res.attributes._CatalogueHierarchy;
                        yield return queryItem;
                    }
                }
            }
        }

        public LanDeskItem GetLatestAssignment(LanDeskItem lanDeskItem)
        {
            var ldToPass = new LanDeskItem
            {
                className = lanDeskItem.attributes.Class + "Management.Assignment",
                value = lanDeskItem.attributes.LatestAssignment
            };
            return ToLanDesk(OpenLanDeskItemSteps, ldToPass);
        }

        private IEnumerable<LanDeskItem> ExecuteQuery(HttpClient client, string query)
        {
            var getResult = client.GetAsync(query).Result;
            var getObjects = getResult.Content.ReadAsStringAsync().Result;
            var dataResult = JsonConvert.DeserializeObject<QueryResult>(getObjects);
            return dataResult.objectCount == 0 ? null : dataResult.objects;
        }

        private FormUrlEncodedContent GenerateForm(LanDeskItem lanDeskItem, string function, string message, LanDeskItem functionObject)
        {
            switch (function)
            {
                case "Create " + LanDeskItemType.Request:
                    return null; // TODO: Construct item.
                case "Create " + LanDeskItemType.Incident:
                    return null;// TODO: Construct item.
                case "Create " + LanDeskItemType.Problem:
                    return null;// TODO: Construct item.
            }
            var functionContentArray = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("class_name", functionObject.className),
                new KeyValuePair<string, string>("key", functionObject.value),
                new KeyValuePair<string, string>("parent_class_name", lanDeskItem.className),
                new KeyValuePair<string, string>("parent_key", lanDeskItem.value),
                new KeyValuePair<string, string>("is_new", "True"),
                new KeyValuePair<string, string>("parent_function_name", function),
                new KeyValuePair<string, string>("CreationDate", functionObject.attributes.CreationDate),
                new KeyValuePair<string, string>("CreationUser", functionObject.attributes.CreationUser)
            };
            switch (function)
            {
                case "Authorise":
                    functionContentArray.AddRange(new[]
                                {
                                    new KeyValuePair<string, string>("Text", message)
                                });
                    break;
                case "Resolve":
                    switch (lanDeskItem.className)
                    {
                        case LanDeskItemType.Problem:
                            functionContentArray.AddRange(new[]
                                {
                                    new KeyValuePair<string, string>("Category", "TODO SET ME"), //TODO: Set the Problem Resolution Category for your LanDesk system.
                                    new KeyValuePair<string, string>("Description", message)
                                });
                            break;
                        case LanDeskItemType.Incident:
                            functionContentArray.AddRange(new[]
                                {
                                    new KeyValuePair<string, string>("_NotifyOriginator", "True"),
                                    new KeyValuePair<string, string>("_CopyCategory", "Development"),
                                    new KeyValuePair<string, string>("Category", "TODO SET ME"), //TODO: Set the Incident Resolution Category for your LanDesk system.
                                    new KeyValuePair<string, string>("Description", message)
                                });
                            break;
                    }

                    break;
                case "AddSystemCause":
                    functionContentArray.AddRange(new[]
                        {
                            new KeyValuePair<string, string>("Description", message),
                            new KeyValuePair<string, string>("LastUpdate", functionObject.attributes.LastUpdate),
                            new KeyValuePair<string, string>("LastUpdateUser", functionObject.attributes.LastUpdateUser)
                        });
                    break;
                case "Complete":
                    string completedTaskCategory;
                    switch (lanDeskItem.className)
                    {
                        case LanDeskItemType.Request:
                            completedTaskCategory = "TODO SET ME"; // TODO: Add your category GUID for completed request status.
                            break;
                        case LanDeskItemType.Incident:
                            completedTaskCategory = "TODO SET ME"; // TODO: Add your category GUID for completed incident status.
                            break;
                        case LanDeskItemType.Problem:
                            completedTaskCategory = "TODO SET ME"; // TODO: Add your category GUID for completed problem status.
                            break;
                        default:
                            throw new Exception("Unexpecxted item type submitted for function Complete. Please review method call and LanDeskItemType.");
                    }
                    functionContentArray.AddRange(new[]
                            {
                                new KeyValuePair<string, string>("Category", completedTaskCategory),
                                new KeyValuePair<string, string>("Description",
                                    "Task completed. Automatically closed by TFS Service.")
                            });
                    break;
                case "AddProvisionConfirmation":
                    functionContentArray.AddRange(new[]
                    {
                        new KeyValuePair<string, string>("_Details", message),
                        new KeyValuePair<string, string>("_CreateDate", functionObject.attributes._CreateDate),
                        new KeyValuePair<string, string>("_CreateUser", functionObject.attributes._CreateUser),
                        new KeyValuePair<string, string>("_UpdateUser", functionObject.attributes._UpdateUser)
                    });
                    functionContentArray.Remove(new KeyValuePair<string, string>("Creation Date", ""));
                    functionContentArray.Remove(new KeyValuePair<string, string>("Creation User", ""));
                    break;
                case "AddDiagnosis":
                    functionContentArray.AddRange(new[]
                                {
                                    new KeyValuePair<string, string>("Text", message),
                                    new KeyValuePair<string, string>("LastUpdate", functionObject.attributes.LastUpdate),
                                    new KeyValuePair<string, string>("LastUpdateUser", functionObject.attributes.LastUpdateUser)
                                });
                    break;
                case "AddAssignment":
                    // TODO Get guids for each. System.SupportGroup & System.Analyst
                    var assignedGroupGuid = GetAssignedGroupIdByName(lanDeskItem.attributes._CurrentAssignedGroup) ?? "";
                    var assigneeGuid = GetAssigneeIdByName(lanDeskItem.attributes._CurrentAssignedAnalyst) ?? "";
                    functionContentArray.AddRange(new[]
                                {
                                    new KeyValuePair<string, string>("Group", assignedGroupGuid),
                                    new KeyValuePair<string, string>("User", assigneeGuid),
                                    new KeyValuePair<string, string>("Description", message)
                                });
                    break;
                case "AddNote":
                    functionContentArray.AddRange(new[]
                                {
                                    new KeyValuePair<string, string>("Text", message)
                                });
                    break;
                case "AddPrivateNote":
                    functionContentArray.AddRange(new[]
                                {
                                    new KeyValuePair<string, string>("_Details", message),
                                    new KeyValuePair<string, string>("_CreateDate", functionObject.attributes._CreateDate),
                                    new KeyValuePair<string, string>("_CreateUser", functionObject.attributes._CreateUser)
                                });
                    functionContentArray.Remove(new KeyValuePair<string, string>("Creation Date", ""));
                    functionContentArray.Remove(new KeyValuePair<string, string>("Creation User", ""));
                    break;
                default:
                    throw new Exception("Unexpected Function call.");
            }
            return new FormUrlEncodedContent(functionContentArray);
        }

        private string GetAssignedGroupIdByName(string currentAssignedGroup)
        {
            var queryBuilder = new QueryBuilder(new LanDeskItemType("Group"));
            var condition = new Condition
            {
                Operator = Operator.Equals,
                Property = "Title",
                Variable = currentAssignedGroup
            };
            var query = queryBuilder.CreateQuery(50, "Guid", condition, "Title", "Name", "Guid");
            var assignedGroupPotential = ToLanDesk(ExecuteQuery, query);
            var assignedGroup = assignedGroupPotential?.FirstOrDefault();
            return assignedGroup?.value;
        }

        private string GetAssigneeIdByName(string currentAssignedAnalyst)
        {
            var queryBuilder = new QueryBuilder(new LanDeskItemType("Analyst"));
            if (string.IsNullOrEmpty(currentAssignedAnalyst)) return null;
            var condition = new Condition
            {
                Operator = Operator.Equals,
                Property = "Name",
                Variable = currentAssignedAnalyst.Substring(0, 2)
            };
            var query = queryBuilder.CreateQuery(50, "Guid", condition, "Title", "Name", "Guid");
            var assigneePotential = ToLanDesk(ExecuteQuery, query);
            var assignee = assigneePotential.FirstOrDefault(ap => ap.name.Contains(currentAssignedAnalyst));
            return assignee?.value;
        }
    }

    public class QueryBuilder
    {
        private readonly Uri _baseUri;
        private readonly LanDeskItemType _lanDeskItemType;
        private readonly string _relativeUri;

        public QueryBuilder(string baseUrl, string relativeUrl, LanDeskItemType lanDeskItemType)
        {
            _baseUri = new Uri(new Uri(baseUrl), new Uri(relativeUrl, UriKind.Relative));
            _lanDeskItemType = lanDeskItemType;
            _relativeUri = relativeUrl;
        }

        public QueryBuilder(string baseUrl, LanDeskItemType lanDeskItemType) : this(baseUrl, "wd/query/list.rails", lanDeskItemType)
        {
        }

        public QueryBuilder(LanDeskItemType lanDeskItemType) : this("wd/query/list.rails", lanDeskItemType) { }

        public uint PageSize { get; set; }

        public string CreateQuery(int pageNumber, string sortProperty, params string[] selectedProperties)
        {
            return CreateQuery(pageNumber, sortProperty, null, selectedProperties);
        }

        public string CreateQuery(int pageNumber, string sortProperty, Condition conditions, params string[] selectedProperties)
        {
            if (selectedProperties.Any(property => !_lanDeskItemType.AvailableProperties.Contains(property)))
            {
                throw new Exception("The property selected isn't available on " + _lanDeskItemType.Name);
            }

            if (!_lanDeskItemType.AvailableProperties.Contains(sortProperty))
                throw new Exception("The sort property isn't available on " + _lanDeskItemType.Name);

            var builder = new UriBuilder(_baseUri);
            var query = new Dictionary<string,string>();
            query["class_name"] = _lanDeskItemType.Name;
            query["page_size"] = PageSize.ToString();
            query["sort_by"] = sortProperty;

            if (!selectedProperties.Any())
                query["attributes"] = string.Join(",", _lanDeskItemType.AvailableProperties.Take(40));
            else
                query["attributes"] = string.Join(",", selectedProperties);

            if (conditions != null)
            {
                query["cns"] = conditions.GetCns();

                foreach (var cn in conditions.GetVariables())
                {
                    query[cn.Name] = cn.Value;
                }
            }

            builder.Query = new QueryExtension(query).ToString();
            var url = builder.ToString();
            return url.Substring(url.IndexOf(_relativeUri));
        }
    }

    public class QueryExtension
    {
        private readonly Dictionary<string, string> _paramaters;
         
        public QueryExtension(Dictionary<string,string> paramaters)
        {
            _paramaters = paramaters;
        }

        public override string ToString()
        {
            return string.Join("&", _paramaters.Select(p => p.Key + "=" + p.Value));
        }
    }

    public class Condition
    {
        public string Property { get; set; }
        public Operator Operator { get; set; }
        public string Variable { get; set; }
        protected int? Index { get; set; }
        public virtual string GetCns()
        {
            return GetCns(0);
        }

        public virtual string GetCns(int index)
        {
            Index = index;
            switch (Operator)
            {
                case (Operator.GreaterThan):
                    return Property + "-gt-" + index;
                case (Operator.Equals):
                    return Property + "-c-" + index;
                case (Operator.LessThan):
                    return Property + "-lt-" + index;
                default:
                    throw new Exception("Unknown operator");
            }
        }

        public virtual int GetCount()
        {
            return 1;
        }

        public virtual List<NameValuePair> GetVariables()
        {
            if (Index == null)
                throw new Exception("Cns must be generated before variables");

            return new List<NameValuePair> { new NameValuePair { Name = "c" + Index, Value = Variable } };
        }
    }

    public class NameValuePair
    {
        public string Name;
        public string Value;
    }

    public class BooleanCondition : Condition
    {
        public Condition Condition1 { get; set; }
        public Condition Condition2 { get; set; }
        public ConditionType ConditionType { get; set; }
        public override string GetCns()
        {
            return GetCns(0);
        }

        public override string GetCns(int index)
        {
            Index = index;
            string conditionSeparator;
            switch (ConditionType)
            {
                case (ConditionType.And):
                    conditionSeparator = "_a_";
                    break;
                case (ConditionType.Or):
                    conditionSeparator = "_o_";
                    break;
                default:
                    throw new Exception("ConditionType not supported");
            }

            if (index == 0)
                return Condition1.GetCns(index) + conditionSeparator + Condition2.GetCns(index + Condition1.GetCount());

            return "(" + Condition1.GetCns(index) + conditionSeparator + Condition2.GetCns(index + Condition1.GetCount()) + ")";
        }

        public override int GetCount()
        {
            return Condition1.GetCount() + Condition2.GetCount();
        }

        public override List<NameValuePair> GetVariables()
        {
            return Condition1.GetVariables().Union(Condition2.GetVariables()).ToList();
        }
    }

    public enum Operator
    {
        GreaterThan,
        LessThan,
        Equals
    }

    public enum ConditionType
    {
        And,
        Or
    }
}
