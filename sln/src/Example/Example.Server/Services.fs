namespace Example.Server

open Microsoft.AspNetCore.DataProtection

/// Each host owns one ephemeral demo-session key ring, reset when that host restarts.
type ServerServices =
    { config: ServerConfig
      sessionProtection: IDataProtectionProvider }

module Services =
    let create config =
        { config = config
          sessionProtection = new EphemeralDataProtectionProvider() }
