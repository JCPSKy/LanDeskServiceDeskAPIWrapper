using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanDeskServiceDeskAPIWrapper
{
    public static class Constants
    {
        // States
        public const string NewState = "New";
        public const string ActiveState = "Active";
        public const string AwaitingServiceDeskReviewState = "Awaiting Service Desk Review";
        public const string ClosedState = "Closed";
        public const string CancelledState = "Cancelled";
        public const string RejectedState = "Rejected";
        public const string RemovedState = "Removed";
        public const string ResolvedState = "Resolved";
        public const string CompletedState = "Completed";
        public const string CompleteState = "Complete";
    }
}
