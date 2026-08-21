using System;
using Xunit;

namespace Dbarone.Net.Database;

public class ThriftCompactProtocolCodecTests
{
  private byte[] Base64ToByteArray(string base64)
  {
    return Convert.FromBase64String(base64);
  }

  private IBuffer Base64ToIBuffer(string base64)
  {
    return new GenericBuffer(Base64ToByteArray(base64));
  }

  [Theory]
  [InlineData("FQIZLEgEcm9vdBUCABUCJQAYA2ZvbyUiTKwTIBEAAAAWChkcGRwmCBwVAhklCgYZGANmb28VAhYKFqYBFnImCDwYBAUAAAAYBAEAAAAWACgEBQAAABgEAQAAAAAAABamARYKNnIAKEpQYXJxdWV0Lk5ldCB2ZXJzaW9uIDUuNS4wIChidWlsZCA0YjA4ZWNkY2ViZjNlM2E3MWU0MmFkNDA3MWE2ZTE5MzQ0NTNiZDhmKQA=")]
  public void TestStruct(string input)
  {
    var buf = Base64ToIBuffer(input);
    ThriftCompactProtocolCodec codec = new ThriftCompactProtocolCodec();
    var results = codec.Decode(buf);
  }

  [Theory]
  [InlineData("FQIZLEgEcm9vdBUCABUCJQAYA2ZvbyUiTKwTIBEAAAAWChkcGRwmCBwVAhklCgYZGANmb28VAhYKFqYBFnImCDwYBAUAAAAYBAEAAAAWACgEBQAAABgEAQAAAAAAABamARYKNnIAKEpQYXJxdWV0Lk5ldCB2ZXJzaW9uIDUuNS4wIChidWlsZCA0YjA4ZWNkY2ViZjNlM2E3MWU0MmFkNDA3MWE2ZTE5MzQ0NTNiZDhmKQA=")]
  public void TestThriftMetaData(string input)
  {
    var buf = Base64ToIBuffer(input);
    ThriftMetaDataSerializer ser = new ThriftMetaDataSerializer();
    var results = ser.GetFileMetaData(buf);
  }
}