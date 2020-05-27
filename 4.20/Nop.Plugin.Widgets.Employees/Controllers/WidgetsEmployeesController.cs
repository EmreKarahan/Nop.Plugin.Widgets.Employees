﻿using System.Linq;
using Nop.Plugin.Widgets.Employees.Models;
using Nop.Plugin.Widgets.Employees.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Security;
using Nop.Core;
using Nop.Services.Media;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Widgets.Employees.Controllers
{
    public partial class WidgetsEmployeesController : BasePluginController
    {
        public static string ControllerName = nameof(WidgetsEmployeesController).Replace("Controller", "");
        const string Route = "~/Plugins/Widgets.Employees/Views/Employees/";

        private readonly EmployeesSettings _employeeSettings;
        private readonly IEmployeesService _employeeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly IWorkContext _workContext;
        private readonly IPictureService _pictureService;

        public WidgetsEmployeesController(
            EmployeesSettings employeeSettings,
            IEmployeesService employeeService,
            ISettingService settingService,
            ILocalizationService localizationService,
            IPermissionService permissionService,
            IWorkContext workContext,
            IPictureService pictureService)
        {
            _employeeSettings = employeeSettings;
            _employeeService = employeeService;
            _settingService = settingService;
            _localizationService = localizationService;
            _permissionService = permissionService;
            _workContext = workContext;
            _pictureService = pictureService;
        }

        public IActionResult Index(bool groupByDepartment = true)
        {
            return ListEmployees(showUnpublished: false, groupByDepartment);
        }

        public IActionResult ListAll(bool groupByDepartment = true)
        {
            // Require admin access to see all
            return ListEmployees(showUnpublished: _permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel), groupByDepartment);
        }

        private IActionResult ListEmployees(bool showUnpublished, bool groupByDepartment)
        { 
            var model = new DepartmentEmployeeModel
            {
                IsAdmin = _permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel),
                ShowAll = showUnpublished,
                GroupByDepartment = groupByDepartment
            };

            if (!model.IsAdmin && showUnpublished)
            {
                showUnpublished = false;
            }

            var employesModel = new EmployeesListModel { Department = new DepartmentModel { Name = "" } };

            foreach (var d in _employeeService.GetAllDepartments(showUnpublished))
            {
                if (groupByDepartment)
                {
                    employesModel = new EmployeesListModel { Department = d.ToModel() };
                }
                foreach (var employee in _employeeService.GetEmployeesByDepartmentId(d.Id, showUnpublished))
                {
                    var e = employee.ToModel();
                    e.PhotoUrl = GetPictureUrl(e.PictureId);
                    e.DepartmentPublished = d.Published;
                    e.DepartmentName = d.Name;
                    employesModel.Employees.Add(e);
                }
                if (groupByDepartment)
                {
                    // The Model is a list of departments each with a list of employees
                    model.EmployeesList.Add(employesModel);
                }
            }
            if (!groupByDepartment)
            {
                // The Model is a single-entry list (empty department) with a list of all employees
                model.EmployeesList.Add(employesModel);
            }

            return View($"{Route}List.cshtml", model);
        }

        private string GetPictureUrl(int pictureId, int targetSize = 200)
        {
            return (pictureId > 0)
                ? _pictureService.GetPictureUrl(pictureId, targetSize)
                : _pictureService.GetDefaultPictureUrl(targetSize, Core.Domain.Media.PictureType.Avatar);
        }

        [HttpPost]
        public IActionResult EmployeeList(EmployeeSearchModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel))
                return Content("Access denied");

            var employees = _employeeService.GetAll(showUnpublished:false, searchModel.Page - 1, searchModel.PageSize);
            var model = new EmployeeListModel().PrepareToGrid(searchModel, employees, () =>
            {
                return employees.Select(employee =>
                {
                    var e = employee.ToModel();
                    e.PhotoUrl = GetPictureUrl(e.PictureId);
                    return e;
                });
            });

            return Json(model);
        }

        public IActionResult EmployeeInfo(string id)
        {
            var model = new EmployeeInfoModel
            {
                IsAdmin = _permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel)
            };

            var e = GetEmployeeByIdOrEmailPrefix(id);
            if (e == null)
            {
                return RedirectToAction(nameof(Index), new { area = "" });
            }

            model.Employee = e.ToModel();
            model.Employee.PhotoUrl = (e.PictureId > 0) ? _pictureService.GetPictureUrl(e.PictureId, 200) : null;
            var department = _employeeService.GetDepartmentById(e.DepartmentId);
            if (department != null)
            {
                model.Employee.DepartmentPublished = department.Published;
                model.Employee.DepartmentName = department.Name;
            }
            return View($"{Route}EmployeeInfo.cshtml", model);
        }

        private Domain.Employee GetEmployeeByIdOrEmailPrefix(string id)
        {
            if (int.TryParse(id, out int employeeId))
            {
                return _employeeService.GetById(employeeId);
            }
            return _employeeService.GetByEmailPrefix(id);
        }
    }
}
