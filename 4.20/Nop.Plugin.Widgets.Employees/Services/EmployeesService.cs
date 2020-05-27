using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Plugin.Widgets.Employees.Domain;

namespace Nop.Plugin.Widgets.Employees.Services
{
    public partial class EmployeesService : IEmployeesService
    {
        #region Constants
        private const string EMPLOYEES_ALL_KEY = "Nop.employee.all-{0}-{1}";
        private const string DEPARTMENTS_ALL_KEY = "Nop.department.all-{0}";
        private const string EMPLOYEES_PATTERN_KEY = "Nop.employee.";
        #endregion

        #region Fields
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Department> _departmentRespository;
        private readonly ICacheManager _cacheManager;
        #endregion

        #region Ctor
        public EmployeesService(
            ICacheManager cacheManager,
            IRepository<Employee> employeeRepository,
            IRepository<Department> departmentRespository)
        {
            _cacheManager = cacheManager;
            _employeeRepository = employeeRepository;
            _departmentRespository = departmentRespository;
        }
        #endregion

        #region Methods
        public virtual void DeleteEmployee(Employee employeeRecord)
        {
            if (employeeRecord == null)
                throw new ArgumentNullException("employeeRecord");

            _employeeRepository.Delete(employeeRecord);

            _cacheManager.RemoveByPrefix(EMPLOYEES_PATTERN_KEY);
        }

        public virtual IPagedList<Employee> GetAll(bool showUnpublished, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            string key = string.Format(EMPLOYEES_ALL_KEY, pageIndex, pageSize);
            return _cacheManager.Get(key, () =>
            {
                return new PagedList<Employee>(
                    from employee in _employeeRepository.Table
                         where showUnpublished 
                               ||
                               (employee.Published
                               && (employee.WorkStarted.Date < DateTime.Now.Date)
                               && (!employee.WorkEnded.HasValue || 
                                       (employee.WorkEnded.Value.Year < 2000 || employee.WorkEnded.Value > DateTime.Now.Date)
                               ))
                         orderby employee.Id
                         select employee,
                    pageIndex, 
                    pageSize);
            });
        }

        public virtual Employee GetById(int id)
        {
            if (id == 0)
                return null;

            return _employeeRepository.GetById(id);
        }

        public virtual Employee GetByEmailPrefix(string emailPrefix)
        {
            if (string.IsNullOrWhiteSpace(emailPrefix))
                return null;

            return (from employee in _employeeRepository.Table
                    where employee.Published
                          && !string.IsNullOrWhiteSpace(employee.Email) 
                          && employee.Email.ToLower().StartsWith(emailPrefix.ToLower() + '@')
                    select employee
                   ).FirstOrDefault();
        }

        public virtual void InsertEmployee(Employee employee)
        {
            if (employee == null)
                throw new ArgumentNullException("employee");

            _employeeRepository.Insert(employee);

            _cacheManager.RemoveByPrefix(EMPLOYEES_PATTERN_KEY);
        }

        public virtual IPagedList<Department> GetAllDepartments(bool showUnpublished, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            string key = string.Format(DEPARTMENTS_ALL_KEY, showUnpublished);
            return _cacheManager.Get(key, () =>
            {
                return new PagedList<Department>(
                    from department in _departmentRespository.Table
                    where department.Published || showUnpublished
                    orderby department.DisplayOrder
                    select department,
                    pageIndex,
                    pageSize);
            });
        }

        public virtual void InsertDepartment(Department department)
        {
            if (department == null)
                throw new ArgumentNullException("department");

            _departmentRespository.Insert(department);

            _cacheManager.RemoveByPrefix(DEPARTMENTS_ALL_KEY);
        }

        public virtual void UpdateEmployee(Employee employee)
        {
            if (employee == null)
                throw new ArgumentNullException("employeeRecord");

            _employeeRepository.Update(employee);

            _cacheManager.RemoveByPrefix(EMPLOYEES_PATTERN_KEY);
        }

        public virtual void UpdateDepartment(Department department)
        {
            if (department == null)
                throw new ArgumentNullException("departmentRecord");

            _departmentRespository.Update(department);

            _cacheManager.RemoveByPrefix(DEPARTMENTS_ALL_KEY);
        }

        public virtual IList<Employee> GetEmployeesByDepartmentId(int departmentId, bool showUnpublished = false)
        {
            return (from employee in _employeeRepository.Table
                    where employee.DepartmentId == departmentId
                          && (showUnpublished
                              ||
                              (employee.Published
                               && (employee.WorkStarted.Date < DateTime.Now.Date)
                               && (!employee.WorkEnded.HasValue ||
                                       (employee.WorkEnded.Value.Year < 2000 || employee.WorkEnded.Value > DateTime.Now.Date)
                                  )))
                    orderby employee.Name
                    select employee).ToList();
        }

        public virtual Department GetDepartmentById(int departId)
        {
            return (from sbw in _departmentRespository.Table
                    where sbw.Id == departId
                    orderby sbw.Name
                    select sbw)
                    .FirstOrDefault();
        }
        #endregion
    }
}
