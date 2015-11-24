using System.Collections.Generic;

namespace NewData {
  public class BlendedCourseUser {
    public BlendedCourseUser() {
      CourseDatas = new HashSet<BlendedCourseData>();
    }

    public int Id { get; set; }
    public int CompanyId { get; set; }
    public long LMComId { get; set; }
    public string ProductUrl { get; set; }

    public virtual ICollection<BlendedCourseData> CourseDatas { get; set; }
    public virtual BlendedCompany Company { get; set; }
  }
}
