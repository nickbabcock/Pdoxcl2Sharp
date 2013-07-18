using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Pdoxcl2Sharp.Test {

	[TestFixture]
	class SuperState : IParadoxRead {
		string emperor;

		IDictionary<string, Action<ParadoxParser>> parseStrategy;
		Dictionary<string, string> otherFields;

		[Test]
		public void WhoIsEmperor() {

			parseStrategy = new Dictionary<string, Action<ParadoxParser>>
			                {
			                	{"emperor", x => emperor = x.ReadString()}
			                };
			otherFields = new Dictionary<string, string>();
			var data =
				@"emperor=""SCA""
old_emperor=
{
    id=619
    country=""BOH""
    date=""1378.11.29""
}".ToStream();

			ParadoxParser.Parse(data, TokenCallback);

			Assert.AreEqual("SCA", emperor);

			Dictionary<string, string> expected = new Dictionary<string, string>()
			                                      {
													{"id", "619"},
													{"country", @"""BOH"""},
													{"date", @"""1378.11.29"""}
			                                      };
			CollectionAssert.AreEqual(expected, otherFields);
		}

		public void TokenCallback(ParadoxParser parser, string token) {
			Action<ParadoxParser> temp;
			if (parseStrategy.TryGetValue(token, out temp))
				temp(parser);
			else
				otherFields.Add(token, parser.ReadString());
		}

	}
}
