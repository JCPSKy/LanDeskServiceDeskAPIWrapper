using System.Collections.Generic;
using System.Net.Http;

namespace LanDeskServiceDeskAPIWrapper
{
    public class LanDeskItemType
    {
        public LanDeskItemType(string type)
        {
            switch (type)
            {
                case "Incident":
                    Name = Incident;
                    break;
                case "Problem":
                    Name = Problem;
                    break;
                case "Request":
                    Name = Request;
                    break;
                case "Analyst":
                    Name = Analyst;
                    break;
                case "Group":
                    Name = Group;
                    break;
            }
            AvailableProperties = GetProperties(Name);
        }

        public string Name;
        public string[] AvailableProperties;

        public const string Incident = "IncidentManagement.Incident";
        public const string Problem = "ProblemManagement.Problem";
        public const string Request = "RequestManagement.Request";
        public const string Analyst = "System.Analyst";
        public const string Group = "System.SupportGroup";
        public const string MetaDataClassType = "MetaData.ClassType";

        public static string[] GetProperties(string type)
        {
            switch (type)
            {
                case Incident:
                    return new[]
                    {
                        "Guid",
                        "Title",
                        "Description",
                        "Lifecycle",
                        "Status",
                        "Class",
                        "CreationUser",
                        "CreateGroup",
                        "LastUpdateUser",
                        "UpdateGroup",
                        "RaiseUser",
                        "CreationDate",
                        "LastUpdate",
                        "RaiseDate",
                        "Priority",
                        "Colour",
                        "Order",
                        "ReplyDisplayName",
                        "ReplyEmailAddress",
                        "_ProcessRef",
                        "_SLA",
                        "Id",
                        "CurrentAssignment",
                        "LatestAssignment",
                        "Customer",
                        "Company",
                        "Supplier",
                        "LatestAssignmentGroup",
                        "Category",
                        "Severity",
                        "IsBreached",
                        "HasMismatch",
                        "ResponseLevel",
                        "IgnoreAgreements",
                        "ClockStopped",
                        "IsClockStopped",
                        "PreviousStoppedTime",
                        "BreachTime",
                        "ConfigurationItem",
                        "ConfigurationItemType",
                        "ConfigurationItemTypeReference",
                        "UsersAffected",
                        "DowntimeHours",
                        "ServiceAffected",
                        "NmsReference",
                        "_SuggestedGroup",
                        "_Group",
                        "_Impact",
                        "_IncidentUrgency",
                        "_SurveyRequired",
                        "_ResolveOnCreation",
                        "_CloseOnCreation",
                        "_Reopened",
                        "_Unresolved",
                        "_MajorIncident",
                        "_LockVersion",
                        "_CurrentAssignedAnalyst",
                        "_CurrentAssignedGroup",
                        "_AssignCount",
                        "_MinutesToBreach",
                        "_IncidentSource",
                        "_AutoCloseFlag",
                        "_FunctionalTeam1",
                        "_DisableCIFilter",
                        "_RaiseUserLocation",
                        "_AlternativeContactUser",
                        "_AlternativeContactPhone",
                        "_AlternativeContactLocation",
                        "_AlternativeContactRoom",
                        "_PartNumber",
                        "_AssetSerialNumber",
                        "_AssetJCPSAssetTag",
                        "_JCPSAssetTag"
                    };
                case Problem:
                    return new[] {
                        "Guid",
                        "Title",
                        "Description",
                        "Lifecycle",
                        "Status",
                        "Class",
                        "CreationUser",
                        "CreateGroup",
                        "LastUpdateUser",
                        "UpdateGroup",
                        "RaiseUser",
                        "CreationDate",
                        "LastUpdate",
                        "RaiseDate",
                        "Priority",
                        "Colour",
                        "Order",
                        "ReplyDisplayName",
                        "ReplyEmailAddress",
                        "_ProcessRef",
                        "_SLA",
                        "Id",
                        "CurrentAssignment",
                        "LatestAssignment",
                        "Customer",
                        "LatestAssignmentGroup",
                        "Category",
                        "Severity",
                        "IsBreached",
                        "HasMismatch",
                        "ResponseLevel",
                        "IgnoreAgreements",
                        "ClockStopped",
                        "IsClockStopped",
                        "PreviousStoppedTime",
                        "BreachTime",
                        "Cause",
                        "ConfigurationItem",
                        "ConfigurationItemType",
                        "ConfigurationItemTypeReference",
                        "_CurrentAssignedGroup",
                        "_CurrentAssignedAnalyst",
                        "_ProblemImpact",
                        "_ProblemUrgency",
                        "_DisableCIFilter",
                        "_AlternativeContactName",
                        "_AlternativeContactPhone",
                        "_AlternativeContactLocation",
                        "_AlternativeContactRoom"
                    };
                case Request:
                    return new[] {
                        "Guid",
                        "Title",
                        "Description",
                        "Lifecycle",
                        "Status",
                        "Class",
                        "CreationUser",
                        "CreateGroup",
                        "LastUpdateUser",
                        "UpdateGroup",
                        "RaiseUser",
                        "CreationDate",
                        "LastUpdate",
                        "RaiseDate",
                        "Priority",
                        "Colour",
                        "Order",
                        "ReplyDisplayName",
                        "ReplyEmailAddress",
                        "_ProcessRef",
                        "_SLA",
                        "Id",
                        "CurrentAssignment",
                        "LatestAssignment",
                        "Customer",
                        "Company",
                        "Supplier",
                        "LatestAssignmentGroup",
                        "Category",
                        "Severity",
                        "IsBreached",
                        "HasMismatch",
                        "ResponseLevel",
                        "IgnoreAgreements",
                        "ClockStopped",
                        "IsClockStopped",
                        "PreviousStoppedTime",
                        "BreachTime",
                        "ConfigurationItem",
                        "_ConfigItemRequested",
                        "ConfigurationItemType",
                        "ConfigurationItemTypeReference",
                        "_RequestedBy",
                        "_CatalogueHierarchy",
                        "_ALMCostCenter",
                        "_ALMFinanceGroup",
                        "_ALMHardwareItem1",
                        "_ALMHardwareSeries",
                        "_ALMRequestId",
                        "_ALMRequestStatus",
                        "_ADDisplayName",
                        "_ADGivenname",
                        "_ADLoginName",
                        "_ADPassword",
                        "_ADSurname",
                        "_ADGroupName",
                        "_IsParentBundleProcess",
                        "_Bundle",
                        "_CurrentAssignedGroup",
                        "_CurrentAssignedAnalyst",
                        "_IMAGEMachineName",
                        "_IMAGERoomNumber",
                        "_IMAGEModel",
                        "_IMAGENumberofComputerstoImage",
                        "_IMAGEWindowsVersion",
                        "_IMAGEManufacturer",
                        "_CALCRequestForm",
                        "_Impact",
                        "_Urgency",
                        "_DisableCIFilter",
                        "_DueDate"};
                case Analyst:
                    return new[]
                    {
                        "Guid",
                        "Title",
                        "Name"
                    };
                case Group:
                    return new[]
                    {
                        "Guid",
                        "Title",
                        "Name"
                    };
            }
            return null;
        }
    }

    public class QueryResult
    {
        public int objectCount;
        public List<LanDeskItem> objects;
        public int pageCount;
        public int pageNumber;
        public int pageSize;
    }

    public class LanDeskItem
    {
        public LanDeskItem()
        {
            attributes = new Attributes();
        }
        public bool activated;
        public string className;
        public Attributes attributes;
        public string name;
        public bool selected;
        public string value;
        public bool Success;
        public string Function;
        public FormUrlEncodedContent FormContent;
        public LanDeskItemType Type;
        IEnumerable<LanDeskItem> ChildTasks;
    }

    public class Attributes
    {
        public string Guid;
        public string Title;
        public string Description;
        public string Lifecycle;
        public string Status;
        public string Class;
        public string CreationUser;
        public string CreateGroup;
        public string LastUpdateUser;
        public string UpdateGroup;
        public string RaiseUser;
        public string CreationDate;
        public string LastUpdate;
        public string RaiseDate;
        public string Priority;
        public string Colour;
        public string Order;
        public string ReplyDisplayName;
        public string ReplyEmailAddress;
        public string _ProcessRef;
        public string _SLA;
        public string Id;
        public string CurrentAssignment;
        public string LatestAssignment;
        public string Customer;
        public string Company;
        public string Supplier;
        public string LatestAssignmentGroup;
        public string Category;
        public string Severity;
        public string IsBreached;
        public string HasMismatch;
        public string ResponseLevel;
        public string IgnoreAgreements;
        public string ClockStopped;
        public string IsClockStopped;
        public string PreviousStoppedTime;
        public string BreachTime;
        public string ConfigurationItem;
        public string ConfigurationItemType;
        public string ConfigurationItemTypeReference;
        public string UsersAffected;
        public string DowntimeHours;
        public string ServiceAffected;
        public string NmsReference;
        public string _SuggestedGroup;
        public string _Group;
        public string _Impact;
        public string _IncidentUrgency;
        public string _SurveyRequired;
        public string _ResolveOnCreation;
        public string _CloseOnCreation;
        public string _Reopened;
        public string _Unresolved;
        public string _MajorIncident;
        public string _LockVersion;
        public string _CurrentAssignedAnalyst;
        public string _CurrentAssignedGroup;
        public string _AssignCount;
        public string _MinutesToBreach;
        public string _IncidentSource;
        public string _AutoCloseFlag;
        public string _FunctionalTeam1;
        public string _DisableCIFilter;
        public string _RaiseUserLocation;
        public string _AlternativeContactUser;
        public string _AlternativeContactPhone;
        public string _AlternativeContactLocation;
        public string _AlternativeContactRoom;
        public string _PartNumber;
        public string _AssetSerialNumber;
        public string _ProblemImpact;
        public string _ProblemUrgency;
        public string _RequestedBy;
        public string _CatalogueHierarchy;
        public string _ALMCostCenter;
        public string _ALMFinanceGroup;
        public string _ALMHardwareItem1;
        public string _ALMHardwareSeries;
        public string _ALMRequestId;
        public string _ALMRequestStatus;
        public string _ADDisplayName;
        public string _ADGivenname;
        public string _ADLoginName;
        public string _ADPassword;
        public string _ADSurname;
        public string _ADGroupName;
        public string _IsParentBundleProcess;
        public string _Bundle;
        public string _IMAGEMachineName;
        public string _IMAGERoomNumber;
        public string _IMAGEModel;
        public string _IMAGENumberofComputerstoImage;
        public string _IMAGEWindowsVersion;
        public string _IMAGEManufacturer;
        public string _CALCRequestForm;
        public string _Urgency;
        public string _DueDate;
        public string _CreateDate;
        public string _CreateUser;
        public string _UpdateUser;
    }
}
