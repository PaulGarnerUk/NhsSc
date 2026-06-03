using Nhs.Sc;

for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--dedupe")
    {
        if (i + 1 >= args.Length || args[i + 1].StartsWith("--"))
        {
            Console.Error.WriteLine("Usage: nhs.sc --dedupe <spreadsheet>");
            return 1;
        }

        try
        {
            new DedupeCommand().Execute(args[i + 1]);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        return 0;
    }
}

Console.Error.WriteLine("Usage: nhs.sc --dedupe <spreadsheet>");
return 1;
