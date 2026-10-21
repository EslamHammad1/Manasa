namespace Manasa.Models;

public class SubjectTeacher
{
    public int SubjectId { get; set; }

    public int TeacherProfileId { get; set; }

    public Subject Subject { get; set; } = null!;

    public TeacherProfile TeacherProfile { get; set; } = null!;
}
