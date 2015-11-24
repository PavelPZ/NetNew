using System.Collections.Generic;

namespace NewData {
  public class BlendedCompany {
    public BlendedCompany() {
      CourseUsers = new HashSet<BlendedCourseUser>();
    }
    public int Id { get; set; }
    public string LearningData { get; set; }
    public virtual ICollection<BlendedCourseUser> CourseUsers { get; set; }
  }
}
