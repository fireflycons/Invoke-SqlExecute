using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SqlExecuteTests.SqlServerIntegration.LocalDb
{
    [TestClass]
    public class TestAdventureWorks_LocalDB2016 : TestAdventureWorksBase<LocalDb2016InstanceInfo>
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        /// <value>
        /// The test context.
        /// </value>
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void BuildAdventureWorksDatabase_LocalDB2016()
        {
            this.BuildAdventureWorksDatabase(this.TestContext);
        }
    }
}
