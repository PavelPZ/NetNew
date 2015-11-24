using System;
using System.Collections.Generic;
using System.Data.SqlTypes;

namespace NewData {
  public class CompanyUsers {
    public CompanyUsers() {
      CourseUsers = new HashSet<CourseUsers>();
      Created = SqlDateTime.MinValue.Value;
    }

    public int Id { get; set; }
    public int CompanyId { get; set; }
    public DateTime Created { get; set; }
    public int? DepartmentId { get; set; }
    public string RolePar { get; set; }
    public long Roles { get; set; }
    public long UserId { get; set; }

    public virtual ICollection<CourseUsers> CourseUsers { get; set; }
    public virtual Companies Company { get; set; }
    public virtual CompanyDepartments CompanyDepartment { get; set; }
    public virtual Users User { get; set; }
  }
}
