using EducatITion.DB;
using EducatITion.DB.Models;
using EducatITion.Models;
using EducatITion.Models.Combined;
using EducatITion.Models.Send;
using EducatITion.Models.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Diagnostics;
using System.Reflection;

namespace EducatITion.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private ISession session { get => HttpContext.Session; }
        private ApplicationContext context = new ApplicationContext();
		private string localization { get => session.GetString("localization"); }

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(string localization, string direction)
        {
            if (localization != null)
                session.SetString("localization", localization);
            if (direction == null)
                direction = "0";

            int lastInd = session.GetInt32("selectedCourseInd") ?? 0;
			session.SetInt32("selectedCourseInd", lastInd + int.Parse(direction));

			var viewModel = new CombinedIndexModel()
            {
                RegModel = new RegModel(),
                AuthModel = new AuthModel(),
                IndexViewModel = new IndexViewModel(session)
            };

            //DEBUG_DB_ADD_DATA();

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Index(CombinedIndexModel model)
        {
            RegUser(model.RegModel);
            AuthUser(model.AuthModel);
           
            model = new CombinedIndexModel()
            {
                RegModel = new RegModel(),
                AuthModel = new AuthModel(),
                IndexViewModel = new IndexViewModel(session)
            };

            return View(model);
        }

        private bool RegUser(RegModel model)
        {
            bool isValid = IsValid(model);

            if (isValid)
            {
                if (model.Password != model.RepeatPassword)
                    return false;

                if (GetUser(model.Email) != null)
                    return false;

                var user = new User()
                {
                    Name = model.Name,
                    Email = model.Email,
                    Password = model.Password,
                    Role = (Role)Enum.Parse(typeof(Role), session.GetString("role"))
                };

                using (var context = new ApplicationContext())
                {
                    context.Users.Add(user);
                    context.SaveChanges();
                }
               
                WriteUserToSession(user);
            }

            return isValid;
        }

        private bool AuthUser(AuthModel model)
        {
            bool isValid = IsValid(model);

            if (isValid)
            {
                var user = GetUser(model.Email);
                if (user != null && user.Password == model.Password)
                    WriteUserToSession(user);
                else
                    return false;
            }

            return isValid;
        }

        private User? GetUser(string email)
        {
            return context.Users.Where(x => x.Email == email).FirstOrDefault();
        }

        private void WriteUserToSession(User user)
        {
            session.SetString("username", user.Name);
            session.SetString("login", user.Email);
            session.SetString("role", user.Role.ToString());
        }

        private bool IsValid<T>(T model)
        {
            if (model == null)
                return false;

            Type modelType = typeof(T);

            bool isValid = true;
            var propertiesNames = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.Name != "Id")
                .Select(x => $"{modelType.Name}.{x.Name}")
                .ToList();

            foreach (var name in propertiesNames)
                isValid &= ModelState[name]?.ValidationState == ModelValidationState.Valid;

            return isValid;
        }

        [HttpPost]
        [Route("Home/SaveChoice")]
        public IActionResult SaveChoice([FromBody] ChoiceModel model)
        {
            session.SetString("role", model.choice);
            return Ok();
        }

        private void DEBUG_DB_ADD_DATA()
        {
            var users = new List<User>
            {
                new User { Email = "Doe1@gmail.com", Password = "123", Role = Role.teacher },
                new User { Email = "Doe2@gmail.com", Password = "123", Role = Role.student },
                new User { Email = "Doe3@gmail.com", Password = "123", Role = Role.student }
            };

            var courses = new List<Course>
            {
                new Course { NameRu = "PythonRu", NameEn = "PythonEn", DescriptionRu = "описание1", DescriptionEn = "desc1", Price = "$100-$151" },
                new Course { NameRu = "CssRu", NameEn = "CssEn", DescriptionRu = "описание2", DescriptionEn = "desc2", Price = "$100-$152" },
                new Course { NameRu = "HtmlRu", NameEn = "HtmlEn", DescriptionRu = "описание3", DescriptionEn = "desc3", Price = "$100-$153" },
                new Course { NameRu = "CssRu", NameEn = "CssEn", DescriptionRu = "описание4", DescriptionEn = "desc2", Price = "$100-$154" },
                new Course { NameRu = "HtmlRu", NameEn = "HtmlEn", DescriptionRu = "описание5", DescriptionEn = "desc3", Price = "$100-$155" }
            };

            using (var context = new ApplicationContext())
            {
                context.Users.AddRange(users);
                context.Courses.AddRange(courses);
                context.SaveChanges();
            }
        }

        public IActionResult Menu(string page)
        {
            page = page ?? "Сourses";
            MenuPage type = (MenuPage)Enum.Parse(typeof(MenuPage), page);

            var currentUser = GetUser(session.GetString("login"));

            if (type == MenuPage.Exit)
            {
                session.Clear();
                return RedirectToAction("Index");
            }

			if (!currentUser.WasInMenu)
            {
                type = MenuPage.Greetings;
                currentUser.WasInMenu = true;
                context.SaveChanges();
            }

			var model = new MenuViewModel(session, type);
            return View(model);
        }

		[HttpGet]
		public IActionResult Catalogue()
		{
            var viewModel = new CombinedCatalogueModel
            {
                SearchModel = new SearchModel(),
                CatalogueViewModel = new CatalogueViewModel(session)
            };
			return View(viewModel);
		}

        [HttpPost]
        public IActionResult Catalogue(CombinedCatalogueModel model, string sort)
        {
            session.SetString("sort", sort ?? "");
            session.SetString("searched", model.SearchModel?.Searched ?? "");

			var viewModel = new CombinedCatalogueModel
			{
				SearchModel = new SearchModel(),
				CatalogueViewModel = new CatalogueViewModel(session)
			};

			return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
