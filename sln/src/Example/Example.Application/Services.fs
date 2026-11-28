namespace Example.Application.Web

open Microsoft.AspNetCore.DataProtection

/// Each host owns one ephemeral demo-session key ring, reset when that host restarts.
type ServerServices =
    { config: ServerConfig
      sessionPath: string
      sessionProtection: IDataProtectionProvider }

module Services =
    let create config sessionPath =
        { config = config
          sessionPath = sessionPath
          sessionProtection = new EphemeralDataProtectionProvider() }
