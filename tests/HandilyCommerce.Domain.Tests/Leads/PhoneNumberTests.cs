using HandilyCommerce.Domain.Leads;

namespace HandilyCommerce.Domain.Tests.Leads;

public class PhoneNumberTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("  ", "")]
    [InlineData("(11) 91234-5678", "11912345678")]
    [InlineData("+55 11 91234-5678", "11912345678")]
    [InlineData("5511", "5511")]
    public void Normalize_KeepsDigits(string? input, string expected) =>
        Assert.Equal(expected, PhoneNumber.Normalize(input));

    [Theory]
    [InlineData("11912345678", true)]
    [InlineData("1131234567", true)]
    [InlineData("0131234567", false)]
    [InlineData("113123456", false)]
    [InlineData("119123456789", false)]
    [InlineData("11a1234567", false)]
    public void IsValid_ChecksLength(string digits, bool expected) =>
        Assert.Equal(expected, PhoneNumber.IsValid(digits));

    [Theory]
    [InlineData("11912345678", "(11) 91234-5678")]
    [InlineData("1131234567", "(11) 3123-4567")]
    [InlineData("123", "123")]
    [InlineData("11a12345678", "11a12345678")]
    [InlineData("11a1234567", "11a1234567")]
    public void Format_PrettyPrints(string digits, string expected) =>
        Assert.Equal(expected, PhoneNumber.Format(digits));
}
