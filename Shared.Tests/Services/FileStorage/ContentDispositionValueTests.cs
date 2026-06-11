using Shared.Services.FileStorage;

namespace Shared.Tests.Services.FileStorage;

[TestFixture]
[Category("Unit")]
public class ContentDispositionValueTests
{
    [Test]
    public void Inline_WithAsciiFileName_QuotesFileName()
    {
        var result = ContentDispositionValue.Inline("report.pdf");

        Assert.That(result, Is.EqualTo("inline; filename=\"report.pdf\""));
    }

    [Test]
    public void Inline_WithNonAsciiFileName_AddsRfc5987EncodedVariant()
    {
        var result = ContentDispositionValue.Inline("звіт.pdf");

        Assert.That(result, Is.EqualTo(
            "inline; filename=\"____.pdf\"; filename*=UTF-8''%D0%B7%D0%B2%D1%96%D1%82.pdf"));
    }

    [Test]
    public void Inline_WithQuotesAndBackslashes_SanitizesFallback()
    {
        var result = ContentDispositionValue.Inline("my\"file\\v1.pdf");

        Assert.That(result, Is.EqualTo(
            "inline; filename=\"my_file_v1.pdf\"; filename*=UTF-8''my%22file%5Cv1.pdf"));
    }

    [Test]
    public void Inline_StripsControlCharacters()
    {
        var result = ContentDispositionValue.Inline("a\r\nb.pdf");

        Assert.That(result, Is.EqualTo("inline; filename=\"ab.pdf\""));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Inline_WithMissingFileName_OmitsFileName(string? fileName)
    {
        var result = ContentDispositionValue.Inline(fileName);

        Assert.That(result, Is.EqualTo("inline"));
    }
}
