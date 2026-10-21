using System.ComponentModel.DataAnnotations;
using Manasa.ViewModels.Account;

namespace Manasa.Tests;

[TestClass]
public class RegistrationValidationTests
{
    [TestMethod]
    public void ValidStudentDetailsPassValidation()
    {
        var model = ValidModel();

        Assert.IsTrue(Validate(model).Count == 0);
    }

    [TestMethod]
    public void InvalidEmailIsRejected()
    {
        var model = ValidModel();
        model.Email = "not-an-email";

        Assert.IsTrue(Validate(model).Any(result => result.MemberNames.Contains(nameof(model.Email))));
    }

    [TestMethod]
    public void PasswordMismatchIsRejected()
    {
        var model = ValidModel();
        model.ConfirmPassword = "DifferentPassword1!";

        Assert.IsTrue(Validate(model).Any(result => result.MemberNames.Contains(nameof(model.ConfirmPassword))));
    }

    [TestMethod]
    public void TermsMustBeAccepted()
    {
        var model = ValidModel();
        model.AcceptTerms = false;

        Assert.IsTrue(Validate(model).Any(result => result.MemberNames.Contains(nameof(model.AcceptTerms))));
    }

    [TestMethod]
    public void PublicRegistrationModelDoesNotAcceptUsernameOrRole()
    {
        var propertyNames = typeof(RegisterViewModel).GetProperties().Select(property => property.Name);

        Assert.IsFalse(propertyNames.Contains("UserName"));
        Assert.IsFalse(propertyNames.Contains("Role"));
    }

    private static RegisterViewModel ValidModel() => new()
    {
        FullName = "Student Example",
        Email = "student@example.com",
        PhoneNumber = "+1 555 0100",
        Password = "Password1!",
        ConfirmPassword = "Password1!",
        AcceptTerms = true
    };

    private static List<ValidationResult> Validate(RegisterViewModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
