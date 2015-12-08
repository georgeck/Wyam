﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Wyam.Common;
using Wyam.Common.Documents;
using Wyam.Common.Pipelines;
using Wyam.Common.Tracing;
using Wyam.Core;
using Wyam.Core.Modules;
using Wyam.Core.Modules.IO;
using Wyam.Core.Modules.Metadata;

namespace Wyam.Modules.Razor.Tests
{
    [TestFixture]
    public class RazorFixture
    {
        [Test]
        public void SimpleTemplate()
        {
            // Given
            string inputFolder = Path.Combine(Environment.CurrentDirectory, @".\Input");
            if (!Directory.Exists(inputFolder))
            {
                Directory.CreateDirectory(inputFolder);
            }
            IExecutionContext context = Substitute.For<IExecutionContext>();
            context.RootFolder.Returns(Environment.CurrentDirectory);
            context.InputFolder.Returns(inputFolder);
            Engine engine = new Engine();
            engine.Configure();
            context.Assemblies.Returns(engine.Assemblies);
            IDocument document = Substitute.For<IDocument>();
            document.GetStream().Returns(new MemoryStream(Encoding.UTF8.GetBytes(@"@for(int c = 0 ; c < 5 ; c++) { <p>@c</p> }")));
            Razor razor = new Razor();

            // When
            razor.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

            // Then
            document.Received(1).Clone(Arg.Any<string>());
            document.Received().Clone(" <p>0</p>  <p>1</p>  <p>2</p>  <p>3</p>  <p>4</p> ");
        }

        [Test]
        public void Tracing()
        {
            // Given
            string inputFolder = Path.Combine(Environment.CurrentDirectory, @".\Input");
            if (!Directory.Exists(inputFolder))
            {
                Directory.CreateDirectory(inputFolder);
            }
            ITrace trace = Substitute.For<ITrace>();
            IExecutionContext context = Substitute.For<IExecutionContext>();
            context.RootFolder.Returns(Environment.CurrentDirectory);
            context.InputFolder.Returns(inputFolder);
            context.Trace.Returns(trace);
            Engine engine = new Engine();
            engine.Configure();
            context.Assemblies.Returns(engine.Assemblies);
            IDocument document = Substitute.For<IDocument>();
            document.GetStream().Returns(new MemoryStream(Encoding.UTF8.GetBytes(@"@{ ExecutionContext.Trace.Information(""Test""); }")));
            Razor razor = new Razor();

            // When
            razor.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

            // Then
            ITrace receivedTrace = context.Received().Trace; // Need to assign to dummy var to keep compiler happy
            trace.Received().Information("Test");
        }

        [Test]
        public void Metadata()
        {
            // Given
            string inputFolder = Path.Combine(Environment.CurrentDirectory, @".\Input");
            if (!Directory.Exists(inputFolder))
            {
                Directory.CreateDirectory(inputFolder);
            }
            IExecutionContext context = Substitute.For<IExecutionContext>();
            context.RootFolder.Returns(Environment.CurrentDirectory);
            context.InputFolder.Returns(inputFolder);
            Engine engine = new Engine();
            engine.Configure();
            context.Assemblies.Returns(engine.Assemblies);
            IDocument document = Substitute.For<IDocument>();
            document.Metadata["MyKey"].Returns("MyValue");
            document.GetStream().Returns(new MemoryStream(Encoding.UTF8.GetBytes(@"<p>@Metadata[""MyKey""]</p>")));
            Razor razor = new Razor();

            // When
            razor.Execute(new[] { document }, context).ToList();

            // Then
            document.Received(1).Clone(Arg.Any<string>());
            document.Received().Clone("<p>MyValue</p>");
        }

        [Test]
        public void LoadSimpleTemplateFile()
        {
            // Given
            Engine engine = new Engine();
            engine.InputFolder = @"TestFiles\Input\";
            ReadFiles readFiles = new ReadFiles(@"SimpleTemplate\Test.cshtml");
            Razor razor = new Razor();
            Meta meta = new Meta("Content", (x, y) => x.Content);
            engine.Pipelines.Add("Pipeline", readFiles, razor, meta);

            // When
            engine.Execute();

            // Then
            Assert.AreEqual(1, engine.Documents.FromPipeline("Pipeline").Count());
            Assert.AreEqual(@"<p>This is a test</p>", engine.Documents.FromPipeline("Pipeline").First().String("Content"));
        }

        [Test]
        public void LoadLayoutFile()
        {
            // Given
            Engine engine = new Engine();
            engine.InputFolder = @"TestFiles\Input\";
            ReadFiles readFiles = new ReadFiles(@"Layout\Test.cshtml");
            Razor razor = new Razor();
            Meta meta = new Meta("Content", (x, y) => x.Content);
            engine.Pipelines.Add("Pipeline", readFiles, razor, meta);

            // When
            engine.Execute();

            // Then
            Assert.AreEqual(1, engine.Documents.FromPipeline("Pipeline").Count());
            Assert.AreEqual("LAYOUT\r\n\r\n<p>This is a test</p>", engine.Documents.FromPipeline("Pipeline").First().String("Content"));
        }

        [Test]
        public void LoadViewStartAndLayoutFile()
        {
            // Given
            Engine engine = new Engine();
            engine.InputFolder = @"TestFiles\Input\";
            ReadFiles readFiles = new ReadFiles(@"ViewStartAndLayout\Test.cshtml");
            Razor razor = new Razor();
            Meta meta = new Meta("Content", (x, y) => x.Content);
            engine.Pipelines.Add("Pipeline", readFiles, razor, meta);

            // When
            engine.Execute();

            // Then
            Assert.AreEqual(1, engine.Documents.FromPipeline("Pipeline").Count());
            Assert.AreEqual("LAYOUT\r\n<p>This is a test</p>", engine.Documents.FromPipeline("Pipeline").First().String("Content"));
        }

        [Test]
        public void AlternateViewStartPath()
        {
            // Given
            Engine engine = new Engine();
            engine.InputFolder = @"TestFiles\Input\";
            ReadFiles readFiles = new ReadFiles(@"AlternateViewStartPath\Test.cshtml");
            Razor razor = new Razor().WithViewStart(@"AlternateViewStart\_ViewStart.cshtml");
            Meta meta = new Meta("Content", (x, y) => x.Content);
            engine.Pipelines.Add("Pipeline", readFiles, razor, meta);

            // When
            engine.Execute();

            // Then
            Assert.AreEqual(1, engine.Documents.FromPipeline("Pipeline").Count());
            Assert.AreEqual("LAYOUT\r\n<p>This is a test</p>", engine.Documents.FromPipeline("Pipeline").First().String("Content"));
        }

        [Test]
        public void IgnoresUnderscoresByDefault()
        {
            // Given
            Engine engine = new Engine();
            engine.InputFolder = @"TestFiles\Input\";
            ReadFiles readFiles = new ReadFiles(@"IgnoreUnderscores\*.cshtml");
            Razor razor = new Razor();
            Meta meta = new Meta("Content", (x, y) => x.Content);
            engine.Pipelines.Add("Pipeline", readFiles, razor, meta);

            // When
            engine.Execute();

            // Then
            Assert.AreEqual(1, engine.Documents.FromPipeline("Pipeline").Count());
            Assert.AreEqual("LAYOUT\r\n\r\n<p>This is a test</p>", engine.Documents.FromPipeline("Pipeline").First().String("Content"));
        }

        [Test]
        public void AlternateIgnorePrefix()
        {
            // Given
            Engine engine = new Engine();
            engine.InputFolder = @"TestFiles\Input\";
            ReadFiles readFiles = new ReadFiles(@"AlternateIgnorePrefix\*.cshtml");
            Razor razor = new Razor().IgnorePrefix("Ignore");
            Meta meta = new Meta("Content", (x, y) => x.Content);
            engine.Pipelines.Add("Pipeline", readFiles, razor, meta);

            // When
            engine.Execute();

            // Then
            Assert.AreEqual(1, engine.Documents.FromPipeline("Pipeline").Count());
            Assert.AreEqual(@"<p>This is a test</p>", engine.Documents.FromPipeline("Pipeline").First().String("Content"));
        }

        [Test]
        public void RenderLayoutSection()
        {
            // Given
            Engine engine = new Engine();
            engine.InputFolder = @"TestFiles\Input\";
            ReadFiles readFiles = new ReadFiles(@"LayoutWithSection\Test.cshtml");
            Razor razor = new Razor();
            Meta meta = new Meta("Content", (x, y) => x.Content);
            engine.Pipelines.Add("Pipeline", readFiles, razor, meta);

            // When
            engine.Execute();

            // Then
            Assert.AreEqual(1, engine.Documents.FromPipeline("Pipeline").Count());
            Assert.AreEqual("LAYOUT\r\n\r\n<p>Section Content</p>\r\n\r\n\r\n<p>This is a test</p>", engine.Documents.FromPipeline("Pipeline").First().String("Content"));
        }

        [Test]
        public void RenderLayoutSectionOnMultipleExecution()
        {
            // Given
            Engine engine = new Engine();
            engine.InputFolder = @"TestFiles\Input\";
            ReadFiles readFiles = new ReadFiles(@"LayoutWithSection\Test.cshtml");
            Razor razor = new Razor();
            Meta meta = new Meta("Content", (x, y) => x.Content);
            engine.Pipelines.Add("Pipeline", readFiles, razor, meta);

            // When
            engine.Execute();
            engine.Execute();

            // Then
            Assert.AreEqual(1, engine.Documents.FromPipeline("Pipeline").Count());
            Assert.AreEqual("LAYOUT\r\n\r\n<p>Section Content</p>\r\n\r\n\r\n<p>This is a test</p>", engine.Documents.FromPipeline("Pipeline").First().String("Content"));
        }
    }
}
