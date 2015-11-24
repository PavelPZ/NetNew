using System.Collections.Generic;

namespace Course {

  public interface ITask {
    int ID { get; }
    int GroupId { get; }
    string Str1 { get; }
    string Str2 { get; }
    int Int1 { get; }
    int Int2 { get; }
    bool Bool1 { get; }
    string TypeVariant { get; }
    string TaskTypeEnumType { get; }

    IEnumerable<ITaskItem> TaskItem { get; }
    ITaskTypeEnum TaskTypeEnum { get; }
  }

  public interface ITaskItem {
    int Id { get; }
    int TaskId { get; }
    string Str1 { get; }
    string Str2 { get; }
    short Order { get; }
    int Int1 { get; }

    ITask Task { get; }
    IEnumerable<ITaskSubItem> TaskSubItem { get; }
  }

  public interface ITaskSubItem {
    int Id { get; }
    int ItemId { get; }
    string Str1 { get; }
    short Order { get; }
    //string Str2 { get; }

    ITaskItem Item { get; }
  }

  public interface ITaskTypeEnum {
    string Type { get; }
    string Title { get; }
    string Descr { get; }
  }
}