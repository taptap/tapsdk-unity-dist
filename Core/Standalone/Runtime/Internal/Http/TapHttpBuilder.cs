using System;

namespace TapSDK.Core.Standalone.Internal.Http
{
    public class TapHttpBuilder
    {
        private readonly string moduleName;
        private readonly string moduleVersion;

        private string domain = null;

        private ITapHttpSign sign = null;
        private ITapHttpParser parser = null;

        private long connectTimeoutMillis = TapHttp.CONNECT_TIMEOUT_MILLIS;
        private long readTimeoutMillis = TapHttp.READ_TIMEOUT_MILLIS;
        private long writeTimeoutMillis = TapHttp.WRITE_TIMEOUT_MILLIS;

        public TapHttpBuilder(string moduleName, string moduleVersion)
        {
            this.moduleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
            this.moduleVersion = moduleVersion ?? throw new ArgumentNullException(nameof(moduleVersion));
        }

        public TapHttp Build()
        {
            TapHttpConfig httpConfig = new TapHttpConfig
            {
                ModuleName = moduleName,
                ModuleVersion = moduleVersion,
                Domain = domain,
                Sign = sign ?? TapHttpSign.CreateDefaultSign(),
                Parser = parser ?? TapHttpParser.CreateDefaultParser(),
                ConnectTimeoutMillis = connectTimeoutMillis,
                ReadTimeoutMillis = readTimeoutMillis,
                WriteTimeoutMillis = writeTimeoutMillis,
            };
            return new TapHttp(httpConfig);
        }

        public TapHttpBuilder ConnectTimeout(long connectTimeoutMillis)
        {
            this.connectTimeoutMillis = connectTimeoutMillis;
            return this;
        }

        public TapHttpBuilder ReadTimeout(long readTimeoutMillis)
        {
            this.readTimeoutMillis = readTimeoutMillis;
            return this;
        }

        public TapHttpBuilder WriteTimeout(long writeTimeoutMillis)
        {
            this.writeTimeoutMillis = writeTimeoutMillis;
            return this;
        }

        public TapHttpBuilder Domain(string domain)
        {
            this.domain = domain;
            return this;
        }

        public TapHttpBuilder Sign(ITapHttpSign sign)
        {
            this.sign = sign;
            return this;
        }

        public TapHttpBuilder Parser(ITapHttpParser parser)
        {
            this.parser = parser;
            return this;
        }

    }

    internal class TapHttpConfig
    {
        public string ModuleName { get; set; }
        public string ModuleVersion { get; set; }
        public string Domain { get; set; }
        public ITapHttpSign Sign { get; set; }
        public ITapHttpParser Parser { get; set; }
        public ITapHttpParser RetryStrategy { get; set; }
        public long ConnectTimeoutMillis { get; set; }
        public long ReadTimeoutMillis { get; set; }
        public long WriteTimeoutMillis { get; set; }
    }

}