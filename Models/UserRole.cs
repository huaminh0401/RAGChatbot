namespace RAGChatbotMVC.Models;

public static class UserRoles
{
    public const string Student = "Student";
    public const string Teacher = "Teacher";
    public const string Admin = "Admin";

    public static readonly string[] All = { Student, Teacher, Admin };
    public const string StudentTeacherAdmin = Student + "," + Teacher + "," + Admin;
    public const string TeacherAdmin = Teacher + "," + Admin;
}
