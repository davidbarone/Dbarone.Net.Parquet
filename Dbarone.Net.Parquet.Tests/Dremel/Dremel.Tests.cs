using Dbarone.Net.Parquet.Dremel;

public class DremelTests
{

    [Fact]
    public void TestDremel1()
    {
        // Schema in dremel paper:
        // https://research.google/pubs/pub36632/
        var root = new SchemaNode("Document", RepetitionKind.Required)
            .AddChild(
                new SchemaNode("Links", RepetitionKind.Optional)
                    .AddChild(new SchemaNode("Backward", RepetitionKind.Repeated))
                    .AddChild(new SchemaNode("Forward", RepetitionKind.Repeated))
            )
            .AddChild(
                new SchemaNode("Name", RepetitionKind.Repeated)
                    .AddChild(new SchemaNode("Language", RepetitionKind.Repeated)
                        .AddChild(new SchemaNode("Code", RepetitionKind.Required))
                        .AddChild(new SchemaNode("Country", RepetitionKind.Optional))
                    )
                    .AddChild(new SchemaNode("Url", RepetitionKind.Optional)
            )
        );

        var records = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                ["DocId"] = 10,

                ["Links"] = new Dictionary<string, object>
                {
                    ["Forward"] = new List<object> { 20, 40, 60 }
                },

                ["Name"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["Language"] = new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object>
                            {
                                ["Code"] = "en-us",
                                ["Country"] = "us"
                            },
                            new Dictionary<string, object>
                            {
                                ["Code"] = "en"
                            }
                        },
                        ["Url"] = "http://A"
                    },

                    new Dictionary<string, object>
                    {
                        ["Url"] = "http://B"
                    },

                    new Dictionary<string, object>
                    {
                        ["Language"] = new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object>
                            {
                                ["Code"] = "en-gb",
                                ["Country"] = "gb"
                            }
                        }
                    }
                }
            },

            new Dictionary<string, object>
            {
                ["DocId"] = 20,

                ["Links"] = new Dictionary<string, object>
                {
                    ["Forward"] = new List<object> { 80 },
                    ["Backward"] = new List<object> { 10, 30 }
                },

                ["Name"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["Url"] = "http://B"
                    }
                }
            }
        };

        var dremel = new DremelProcessor();

        // Shred
        var buffers = dremel.Shred(root, records);

        // Assemble back
        var reconstructed = dremel.Assemble(root, buffers);

    }

    [Fact]
    public void TestDremel2()
    {
        // Example Dremel-style schema
        var root = new SchemaNode("doc", RepetitionKind.Required)
            .AddChild(
                new SchemaNode("links", RepetitionKind.Repeated)
                    .AddChild(new SchemaNode("url", RepetitionKind.Optional, isLeaf: true))
                    .AddChild(new SchemaNode("language", RepetitionKind.Optional, isLeaf: true))
            );

        // Example records (JSON-like)
        var records = new List<IDictionary<string, object?>>
    {
      new Dictionary<string, object?>
      {
          ["links"] = new List<Dictionary<string, object?>>
          {
              new Dictionary<string, object?>
              {
                  ["url"] = "http://a",
                  ["language"] = "en"
              },
              new Dictionary<string, object?>
              {
                  ["url"] = "http://b",
                  ["language"] = null
              }
          }
      }
    };

        var dremel = new DremelProcessor();

        // Shred
        var buffers = dremel.Shred(root, records);

        // Assemble back
        var reconstructed = dremel.Assemble(root, buffers);
    }
}