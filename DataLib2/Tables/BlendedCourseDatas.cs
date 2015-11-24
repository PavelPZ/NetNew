namespace NewData {
  public class BlendedCourseData {
    public int Id { get; set; }
    public int CourseUserId { get; set; }
    public string Data { get; set; }
    public long Flags { get; set; }
    public string Key { get; set; }
    public string ShortData { get; set; }
    public string TaskId { get; set; }

    public virtual BlendedCourseUser CourseUser { get; set; }
  }
}
