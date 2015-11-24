using System;
using System.Data.SqlTypes;

namespace NewData {
  public class UserLicences {
    public UserLicences() {
      Started = Created = Started = SqlDateTime.MinValue.Value;
    }
    public int Id { get; set; }
    public int LicenceId { get; set; }
    public int Counter { get; set; }
    public DateTime Created { get; set; }
    public DateTime Started { get; set; }
    public int UserId { get; set; }

    public virtual CompanyLicences CompanyLicence { get; set; }
    public virtual CourseUsers CourseUser { get; set; }
  }
}
