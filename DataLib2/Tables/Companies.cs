using System;
using System.Collections.Generic;
using System.Data.SqlTypes;

namespace NewData {
  public class Companies {
    public Companies() {
      CompanyDepartments = new HashSet<CompanyDepartments>();
      CompanyLicences = new HashSet<CompanyLicences>();
      CompanyUsers = new HashSet<CompanyUsers>();
      PublisherOwners = new HashSet<Users>();
      Created = SqlDateTime.MinValue.Value;
    }

    public int Id { get; set; }
    public DateTime Created { get; set; }
    public string HumanEvalPaymentConfig { get; set; }
    public string IntervalsConfig { get; set; }
    public string ScormHost { get; set; }
    public string Title { get; set; }

    public virtual ICollection<CompanyDepartments> CompanyDepartments { get; set; }
    public virtual ICollection<CompanyLicences> CompanyLicences { get; set; }
    public virtual ICollection<CompanyUsers> CompanyUsers { get; set; }
    public virtual ICollection<Users> PublisherOwners { get; set; }
  }
}
