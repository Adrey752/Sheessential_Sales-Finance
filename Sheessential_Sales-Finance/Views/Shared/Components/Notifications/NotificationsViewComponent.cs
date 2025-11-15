using Microsoft.AspNetCore.Mvc;
using Sheessential_Sales_Finance.helpers;
using MongoDB.Driver;
namespace Sheessential_Sales_Finance.Views.Shared.Components.Notifications
{
    public class NotificationsViewComponent : ViewComponent
    {
        private readonly MongoHelper _mongo;

        public NotificationsViewComponent(MongoHelper mongo)
        {
            _mongo = mongo;
        }

        public IViewComponentResult Invoke()
        {
            var userId = HttpContext.Session.GetString("UserId");

            var notifications = _mongo.ActionLog
                .Find(a => a.UserId == userId)
                .SortByDescending(a => a.TimeStamp)
                .Limit(10)
                .ToList();

            return View(notifications);
        }
    }
}
