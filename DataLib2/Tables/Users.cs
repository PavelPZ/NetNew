using System;
using System.Collections.Generic;
using System.Data.SqlTypes;

namespace NewData {
  public class Users {
    public Users() {
      CompanyUsers = new HashSet<CompanyUsers>();
      Created = SqlDateTime.MinValue.Value;
    }

    public long Id { get; set; }
    public DateTime Created { get; set; }
    public string EMail { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Login { get; set; }
    public string LoginEMail { get; set; }
    public int? MyPublisherId { get; set; }
    public string OtherData { get; set; }
    public string OtherId { get; set; }
    public short OtherType { get; set; }
    public string Password { get; set; }
    public long Roles { get; set; }
    public short VerifyStatus { get; set; }

    public virtual ICollection<CompanyUsers> CompanyUsers { get; set; }
    public virtual Companies MyPublisher { get; set; }
  }
}
