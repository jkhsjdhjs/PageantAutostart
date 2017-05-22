namespace PageantAutostart {
    class KeyConfig {
        public string path, pass;
        public KeyConfig(string path = null, string pass = null) {
            this.path = path;
            if(this.path == null)
                this.path = Settings.DefaultPath;
            this.pass = pass;
        }
    }
    class Config {
        public KeyConfig[] keys;
        public Config(KeyConfig[] keys = null) {
            this.keys = keys;
            if(this.keys == null) {
                this.keys = new KeyConfig[] { new KeyConfig() };
            }
        }
    }
}
