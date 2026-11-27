using Dbarone.Net.Parquet.Dremel;
using Dbarone.Net.Parquet.Common;

public class DremelTests
{

    [Fact]
    public void TestDremel1()
    {
        // Schema in dremel paper:
        // https://research.google/pubs/pub36632/
        var root = new Node<LogicalSchemaElement>(new LogicalSchemaElement("Document", DremelKind.Required))
            .AddChild(
                new Node<LogicalSchemaElement>(new LogicalSchemaElement("Links", DremelKind.Optional))
                    .AddChild(new LogicalSchemaElement("Backward", DremelKind.Repeated))
                    .AddChild(new LogicalSchemaElement("Forward", DremelKind.Repeated))
            )
            .AddChild(
                new Node<LogicalSchemaElement>(new LogicalSchemaElement("Name", DremelKind.Repeated))
                    .AddChild(new Node<LogicalSchemaElement>(new LogicalSchemaElement("Language", DremelKind.Repeated))
                        .AddChild(new LogicalSchemaElement("Code", DremelKind.Required))
                        .AddChild(new LogicalSchemaElement("Country", DremelKind.Optional))
                    )
                    .AddChild(new LogicalSchemaElement("Url", DremelKind.Optional)
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
        var buffers = dremel.Shred(root.ConvertTo(LogicalSchemaElementConverter.Convert), records);

        // Assemble back
        var reconstructed = dremel.Assemble(buffers);

    }

    [Fact]
    public void TestDremel2()
    {
        // Example Dremel-style schema
        Node<LogicalSchemaElement> root = ((Node<LogicalSchemaElement>)new LogicalSchemaElement("doc", DremelKind.Required))
            .AddChild(
                ((Node<LogicalSchemaElement>)new LogicalSchemaElement("links", DremelKind.Repeated))
                    .AddChild(new LogicalSchemaElement("url", DremelKind.Optional))
                    .AddChild(new LogicalSchemaElement("language", DremelKind.Optional))
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
        var buffers = dremel.Shred(root.ConvertTo(LogicalSchemaElementConverter.Convert), records);

        // Assemble back
        var reconstructed = dremel.Assemble(buffers);
    }
}