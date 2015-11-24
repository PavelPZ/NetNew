using System;
using System.Collections.Generic;

namespace NewData {
  public class CourseUsers {
    public CourseUsers() {
      CourseDatas = new HashSet<CourseDatas>();
      UserLicences = new HashSet<UserLicences>();
    }

    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime HumanAssigned { get; set; }
    public int HumanCompanyUserId { get; set; }
    public string ProductId { get; set; }
    public int UserId { get; set; }

    public virtual ICollection<CourseDatas> CourseDatas { get; set; }
    public virtual ICollection<UserLicences> UserLicences { get; set; }
    public virtual CompanyUsers CompanyUser { get; set; }
  }
}
