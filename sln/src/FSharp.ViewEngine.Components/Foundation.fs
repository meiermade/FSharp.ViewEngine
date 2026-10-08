namespace FSharp.ViewEngine.Components

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar

module private ThemeClasses =
    let private normalize (value:string) = Regex.Replace(value, "\\s+", " ").Trim()

    let foundation = normalize """
        fve-components [color-scheme:light] dark:not-data-[fve-color-mode=light]:[color-scheme:dark] [.dark_&:not([data-fve-color-mode=light])]:[color-scheme:dark] [&.dark]:[color-scheme:dark] data-[fve-color-mode=dark]:[color-scheme:dark] data-[fve-color-mode=light]:[color-scheme:light]
        [--fve-background:oklch(98.5%_0.002_247.839)] [--fve-surface:oklch(100%_0_0)] [--fve-surface-subtle:oklch(96.7%_0.003_264.542)] [--fve-surface-hover:oklch(96.7%_0.003_264.542)] [--fve-surface-active:oklch(92.8%_0.006_264.531)]
        [--fve-text:oklch(21%_0.034_264.665)] [--fve-muted-text:oklch(44.6%_0.03_256.802)] [--fve-border:oklch(87.2%_0.01_258.338)] [--fve-overlay-backdrop:oklch(13%_0.028_261.692/55%)]
        [--fve-neutral-subtle:oklch(96.7%_0.003_264.542)] [--fve-neutral-text:oklch(37.3%_0.034_259.733)] [--fve-neutral-ring:oklch(70.7%_0.022_261.325)]
        [--fve-positive-subtle:oklch(96.2%_0.044_156.743)] [--fve-positive-text:oklch(44.8%_0.119_151.328)] [--fve-positive-ring:oklch(72.3%_0.219_149.579)]
        [--fve-warning-subtle:oklch(97.3%_0.071_103.193)] [--fve-warning-text:oklch(47.6%_0.114_61.907)] [--fve-warning-ring:oklch(76.9%_0.188_70.08)]
        [--fve-critical-subtle:oklch(97.1%_0.013_17.38)] [--fve-critical-solid:oklch(57.7%_0.245_27.325)] [--fve-critical-hover:oklch(50.5%_0.213_27.518)] [--fve-critical-active:oklch(44.4%_0.177_26.899)] [--fve-critical-text:oklch(44.4%_0.177_26.899)] [--fve-critical-ring:oklch(63.7%_0.237_25.331)]
        [--fve-info-subtle:oklch(97%_0.014_254.604)] [--fve-info-text:oklch(48.8%_0.243_264.376)] [--fve-info-ring:oklch(62.3%_0.214_259.815)]
        [--fve-radius-control:0.5rem] [--fve-radius-panel:0.75rem] [--fve-control-padding-block:0.5rem] [--fve-control-min-height:2.5rem]
        [--fve-popup-active-background:var(--fve-brand-solid)] [--fve-popup-active-text:white]
        dark:not-data-[fve-color-mode=light]:[--fve-popup-active-background:var(--fve-brand-text)] dark:not-data-[fve-color-mode=light]:[--fve-popup-active-text:var(--fve-surface)]
        [.dark_&:not([data-fve-color-mode=light])]:[--fve-popup-active-background:var(--fve-brand-text)] [.dark_&:not([data-fve-color-mode=light])]:[--fve-popup-active-text:var(--fve-surface)]
        [&.dark]:[--fve-popup-active-background:var(--fve-brand-text)] [&.dark]:[--fve-popup-active-text:var(--fve-surface)]
        data-[fve-color-mode=dark]:[--fve-popup-active-background:var(--fve-brand-text)] data-[fve-color-mode=dark]:[--fve-popup-active-text:var(--fve-surface)]
        dark:not-data-[fve-color-mode=light]:[--fve-background:oklch(21%_0.034_264.665)] [.dark_&:not([data-fve-color-mode=light])]:[--fve-background:oklch(21%_0.034_264.665)] [&.dark]:[--fve-background:oklch(21%_0.034_264.665)] data-[fve-color-mode=dark]:[--fve-background:oklch(21%_0.034_264.665)]
        --fve-surface:oklch(24.4%_0.034_260.757) [.dark_&:not([data-fve-color-mode=light])]:[--fve-surface:oklch(24.4%_0.034_260.757)] [&.dark]:[--fve-surface:oklch(24.4%_0.034_260.757)] data-[fve-color-mode=dark]:[--fve-surface:oklch(24.4%_0.034_260.757)]
        --fve-surface-subtle:oklch(27.8%_0.033_256.848) [.dark_&:not([data-fve-color-mode=light])]:[--fve-surface-subtle:oklch(27.8%_0.033_256.848)] [&.dark]:[--fve-surface-subtle:oklch(27.8%_0.033_256.848)] data-[fve-color-mode=dark]:[--fve-surface-subtle:oklch(27.8%_0.033_256.848)]
        --fve-surface-hover:oklch(27.8%_0.033_256.848) [.dark_&:not([data-fve-color-mode=light])]:[--fve-surface-hover:oklch(27.8%_0.033_256.848)] [&.dark]:[--fve-surface-hover:oklch(27.8%_0.033_256.848)] data-[fve-color-mode=dark]:[--fve-surface-hover:oklch(27.8%_0.033_256.848)]
        --fve-surface-active:oklch(37.3%_0.034_259.733) [.dark_&:not([data-fve-color-mode=light])]:[--fve-surface-active:oklch(37.3%_0.034_259.733)] [&.dark]:[--fve-surface-active:oklch(37.3%_0.034_259.733)] data-[fve-color-mode=dark]:[--fve-surface-active:oklch(37.3%_0.034_259.733)]
        --fve-text:oklch(96.7%_0.003_264.542) [.dark_&:not([data-fve-color-mode=light])]:[--fve-text:oklch(96.7%_0.003_264.542)] [&.dark]:[--fve-text:oklch(96.7%_0.003_264.542)] data-[fve-color-mode=dark]:[--fve-text:oklch(96.7%_0.003_264.542)]
        --fve-muted-text:oklch(70.7%_0.022_261.325) [.dark_&:not([data-fve-color-mode=light])]:[--fve-muted-text:oklch(70.7%_0.022_261.325)] [&.dark]:[--fve-muted-text:oklch(70.7%_0.022_261.325)] data-[fve-color-mode=dark]:[--fve-muted-text:oklch(70.7%_0.022_261.325)]
        --fve-border:oklch(37.3%_0.034_259.733) [.dark_&:not([data-fve-color-mode=light])]:[--fve-border:oklch(37.3%_0.034_259.733)] [&.dark]:[--fve-border:oklch(37.3%_0.034_259.733)] data-[fve-color-mode=dark]:[--fve-border:oklch(37.3%_0.034_259.733)]
        --fve-neutral-subtle:oklch(27.8%_0.033_256.848) [.dark_&:not([data-fve-color-mode=light])]:[--fve-neutral-subtle:oklch(27.8%_0.033_256.848)] [&.dark]:[--fve-neutral-subtle:oklch(27.8%_0.033_256.848)] data-[fve-color-mode=dark]:[--fve-neutral-subtle:oklch(27.8%_0.033_256.848)]
        --fve-neutral-text:oklch(87.2%_0.01_258.338) [.dark_&:not([data-fve-color-mode=light])]:[--fve-neutral-text:oklch(87.2%_0.01_258.338)] [&.dark]:[--fve-neutral-text:oklch(87.2%_0.01_258.338)] data-[fve-color-mode=dark]:[--fve-neutral-text:oklch(87.2%_0.01_258.338)]
        --fve-positive-subtle:oklch(26.6%_0.065_152.934) [.dark_&:not([data-fve-color-mode=light])]:[--fve-positive-subtle:oklch(26.6%_0.065_152.934)] [&.dark]:[--fve-positive-subtle:oklch(26.6%_0.065_152.934)] data-[fve-color-mode=dark]:[--fve-positive-subtle:oklch(26.6%_0.065_152.934)]
        --fve-positive-text:oklch(87.1%_0.15_154.449) [.dark_&:not([data-fve-color-mode=light])]:[--fve-positive-text:oklch(87.1%_0.15_154.449)] [&.dark]:[--fve-positive-text:oklch(87.1%_0.15_154.449)] data-[fve-color-mode=dark]:[--fve-positive-text:oklch(87.1%_0.15_154.449)]
        --fve-warning-subtle:oklch(27.9%_0.077_45.635) [.dark_&:not([data-fve-color-mode=light])]:[--fve-warning-subtle:oklch(27.9%_0.077_45.635)] [&.dark]:[--fve-warning-subtle:oklch(27.9%_0.077_45.635)] data-[fve-color-mode=dark]:[--fve-warning-subtle:oklch(27.9%_0.077_45.635)]
        --fve-warning-text:oklch(90.5%_0.182_98.111) [.dark_&:not([data-fve-color-mode=light])]:[--fve-warning-text:oklch(90.5%_0.182_98.111)] [&.dark]:[--fve-warning-text:oklch(90.5%_0.182_98.111)] data-[fve-color-mode=dark]:[--fve-warning-text:oklch(90.5%_0.182_98.111)]
        --fve-critical-subtle:oklch(25.8%_0.092_26.042) [.dark_&:not([data-fve-color-mode=light])]:[--fve-critical-subtle:oklch(25.8%_0.092_26.042)] [&.dark]:[--fve-critical-subtle:oklch(25.8%_0.092_26.042)] data-[fve-color-mode=dark]:[--fve-critical-subtle:oklch(25.8%_0.092_26.042)]
        --fve-critical-text:oklch(80.8%_0.114_19.571) [.dark_&:not([data-fve-color-mode=light])]:[--fve-critical-text:oklch(80.8%_0.114_19.571)] [&.dark]:[--fve-critical-text:oklch(80.8%_0.114_19.571)] data-[fve-color-mode=dark]:[--fve-critical-text:oklch(80.8%_0.114_19.571)]
        --fve-info-subtle:oklch(28.2%_0.091_267.935) [.dark_&:not([data-fve-color-mode=light])]:[--fve-info-subtle:oklch(28.2%_0.091_267.935)] [&.dark]:[--fve-info-subtle:oklch(28.2%_0.091_267.935)] data-[fve-color-mode=dark]:[--fve-info-subtle:oklch(28.2%_0.091_267.935)]
        --fve-info-text:oklch(80.9%_0.105_251.813) [.dark_&:not([data-fve-color-mode=light])]:[--fve-info-text:oklch(80.9%_0.105_251.813)] [&.dark]:[--fve-info-text:oklch(80.9%_0.105_251.813)] data-[fve-color-mode=dark]:[--fve-info-text:oklch(80.9%_0.105_251.813)]
    """

    let sky = normalize """
        fve-theme-sky [--fve-brand-subtle:oklch(97.7%_0.013_236.62)] [--fve-brand-solid:oklch(50%_0.134_242.749)] [--fve-brand-hover:oklch(44.3%_0.11_240.79)] [--fve-brand-active:oklch(39.1%_0.09_240.876)] [--fve-brand-text:oklch(44.3%_0.11_240.79)] [--fve-brand-ring:oklch(55.4%_0.135_241.966)]
        --fve-brand-subtle:oklch(29.3%_0.066_243.157) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-subtle:oklch(29.3%_0.066_243.157)] [&.dark]:[--fve-brand-subtle:oklch(29.3%_0.066_243.157)] data-[fve-color-mode=dark]:[--fve-brand-subtle:oklch(29.3%_0.066_243.157)] --fve-brand-solid:oklch(55.4%_0.135_241.966) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-solid:oklch(55.4%_0.135_241.966)] [&.dark]:[--fve-brand-solid:oklch(55.4%_0.135_241.966)] data-[fve-color-mode=dark]:[--fve-brand-solid:oklch(55.4%_0.135_241.966)] --fve-brand-hover:oklch(65.5%_0.15_237.323) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-hover:oklch(65.5%_0.15_237.323)] [&.dark]:[--fve-brand-hover:oklch(65.5%_0.15_237.323)] data-[fve-color-mode=dark]:[--fve-brand-hover:oklch(65.5%_0.15_237.323)] --fve-brand-active:oklch(74.6%_0.16_232.661) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-active:oklch(74.6%_0.16_232.661)] [&.dark]:[--fve-brand-active:oklch(74.6%_0.16_232.661)] data-[fve-color-mode=dark]:[--fve-brand-active:oklch(74.6%_0.16_232.661)] --fve-brand-text:oklch(82.8%_0.111_230.318) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-text:oklch(82.8%_0.111_230.318)] [&.dark]:[--fve-brand-text:oklch(82.8%_0.111_230.318)] data-[fve-color-mode=dark]:[--fve-brand-text:oklch(82.8%_0.111_230.318)] --fve-brand-ring:oklch(74.6%_0.16_232.661) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-ring:oklch(74.6%_0.16_232.661)] [&.dark]:[--fve-brand-ring:oklch(74.6%_0.16_232.661)] data-[fve-color-mode=dark]:[--fve-brand-ring:oklch(74.6%_0.16_232.661)]
    """

    let emerald = normalize """
        fve-theme-emerald [--fve-brand-subtle:oklch(97.9%_0.021_166.113)] [--fve-brand-solid:oklch(59.6%_0.145_163.225)] [--fve-brand-hover:oklch(50.8%_0.118_165.612)] [--fve-brand-active:oklch(43.2%_0.095_166.913)] [--fve-brand-text:oklch(43.2%_0.095_166.913)] [--fve-brand-ring:oklch(50.8%_0.118_165.612)]
        --fve-brand-subtle:oklch(26.2%_0.051_172.552) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-subtle:oklch(26.2%_0.051_172.552)] [&.dark]:[--fve-brand-subtle:oklch(26.2%_0.051_172.552)] data-[fve-color-mode=dark]:[--fve-brand-subtle:oklch(26.2%_0.051_172.552)] --fve-brand-solid:oklch(56.5%_0.128_163.225) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-solid:oklch(56.5%_0.128_163.225)] [&.dark]:[--fve-brand-solid:oklch(56.5%_0.128_163.225)] data-[fve-color-mode=dark]:[--fve-brand-solid:oklch(56.5%_0.128_163.225)] --fve-brand-hover:oklch(66.5%_0.154_162.48) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-hover:oklch(66.5%_0.154_162.48)] [&.dark]:[--fve-brand-hover:oklch(66.5%_0.154_162.48)] data-[fve-color-mode=dark]:[--fve-brand-hover:oklch(66.5%_0.154_162.48)] --fve-brand-active:oklch(76.5%_0.177_163.223) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-active:oklch(76.5%_0.177_163.223)] [&.dark]:[--fve-brand-active:oklch(76.5%_0.177_163.223)] data-[fve-color-mode=dark]:[--fve-brand-active:oklch(76.5%_0.177_163.223)] --fve-brand-text:oklch(84.5%_0.143_164.978) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-text:oklch(84.5%_0.143_164.978)] [&.dark]:[--fve-brand-text:oklch(84.5%_0.143_164.978)] data-[fve-color-mode=dark]:[--fve-brand-text:oklch(84.5%_0.143_164.978)] --fve-brand-ring:oklch(76.5%_0.177_163.223) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-ring:oklch(76.5%_0.177_163.223)] [&.dark]:[--fve-brand-ring:oklch(76.5%_0.177_163.223)] data-[fve-color-mode=dark]:[--fve-brand-ring:oklch(76.5%_0.177_163.223)]
    """

    let amber = normalize """
        fve-theme-amber [--fve-brand-subtle:oklch(98.7%_0.022_95.277)] [--fve-brand-solid:oklch(66.6%_0.179_58.318)] [--fve-brand-hover:oklch(55.5%_0.163_48.998)] [--fve-brand-active:oklch(47.3%_0.137_46.201)] [--fve-brand-text:oklch(47.3%_0.137_46.201)] [--fve-brand-ring:oklch(55.5%_0.163_48.998)]
        --fve-brand-subtle:oklch(27.9%_0.077_45.635) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-subtle:oklch(27.9%_0.077_45.635)] [&.dark]:[--fve-brand-subtle:oklch(27.9%_0.077_45.635)] data-[fve-color-mode=dark]:[--fve-brand-subtle:oklch(27.9%_0.077_45.635)] --fve-brand-solid:oklch(61%_0.17_55.5) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-solid:oklch(61%_0.17_55.5)] [&.dark]:[--fve-brand-solid:oklch(61%_0.17_55.5)] data-[fve-color-mode=dark]:[--fve-brand-solid:oklch(61%_0.17_55.5)] --fve-brand-hover:oklch(70%_0.18_65) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-hover:oklch(70%_0.18_65)] [&.dark]:[--fve-brand-hover:oklch(70%_0.18_65)] data-[fve-color-mode=dark]:[--fve-brand-hover:oklch(70%_0.18_65)] --fve-brand-active:oklch(80%_0.17_75) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-active:oklch(80%_0.17_75)] [&.dark]:[--fve-brand-active:oklch(80%_0.17_75)] data-[fve-color-mode=dark]:[--fve-brand-active:oklch(80%_0.17_75)] --fve-brand-text:oklch(87.9%_0.169_91.605) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-text:oklch(87.9%_0.169_91.605)] [&.dark]:[--fve-brand-text:oklch(87.9%_0.169_91.605)] data-[fve-color-mode=dark]:[--fve-brand-text:oklch(87.9%_0.169_91.605)] --fve-brand-ring:oklch(76.9%_0.188_70.08) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-ring:oklch(76.9%_0.188_70.08)] [&.dark]:[--fve-brand-ring:oklch(76.9%_0.188_70.08)] data-[fve-color-mode=dark]:[--fve-brand-ring:oklch(76.9%_0.188_70.08)]
    """

    let cyan = normalize """
        fve-theme-cyan [--fve-brand-subtle:oklch(98.4%_0.019_200.873)] [--fve-brand-solid:oklch(60.9%_0.126_221.723)] [--fve-brand-hover:oklch(52%_0.105_223.128)] [--fve-brand-active:oklch(45%_0.085_224.283)] [--fve-brand-text:oklch(45%_0.085_224.283)] [--fve-brand-ring:oklch(52%_0.105_223.128)]
        --fve-brand-subtle:oklch(30.2%_0.056_229.695) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-subtle:oklch(30.2%_0.056_229.695)] [&.dark]:[--fve-brand-subtle:oklch(30.2%_0.056_229.695)] data-[fve-color-mode=dark]:[--fve-brand-subtle:oklch(30.2%_0.056_229.695)] --fve-brand-solid:oklch(60.9%_0.126_221.723) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-solid:oklch(60.9%_0.126_221.723)] [&.dark]:[--fve-brand-solid:oklch(60.9%_0.126_221.723)] data-[fve-color-mode=dark]:[--fve-brand-solid:oklch(60.9%_0.126_221.723)] --fve-brand-hover:oklch(71.5%_0.143_215.221) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-hover:oklch(71.5%_0.143_215.221)] [&.dark]:[--fve-brand-hover:oklch(71.5%_0.143_215.221)] data-[fve-color-mode=dark]:[--fve-brand-hover:oklch(71.5%_0.143_215.221)] --fve-brand-active:oklch(78.9%_0.154_211.53) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-active:oklch(78.9%_0.154_211.53)] [&.dark]:[--fve-brand-active:oklch(78.9%_0.154_211.53)] data-[fve-color-mode=dark]:[--fve-brand-active:oklch(78.9%_0.154_211.53)] --fve-brand-text:oklch(86.5%_0.127_207.078) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-text:oklch(86.5%_0.127_207.078)] [&.dark]:[--fve-brand-text:oklch(86.5%_0.127_207.078)] data-[fve-color-mode=dark]:[--fve-brand-text:oklch(86.5%_0.127_207.078)] --fve-brand-ring:oklch(71.5%_0.143_215.221) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-ring:oklch(71.5%_0.143_215.221)] [&.dark]:[--fve-brand-ring:oklch(71.5%_0.143_215.221)] data-[fve-color-mode=dark]:[--fve-brand-ring:oklch(71.5%_0.143_215.221)]
    """

    let neutral = normalize """
        fve-theme-neutral [--fve-brand-subtle:oklch(96.7%_0.003_264.542)] [--fve-brand-solid:oklch(44.6%_0.03_256.802)] [--fve-brand-hover:oklch(37.3%_0.034_259.733)] [--fve-brand-active:oklch(27.8%_0.033_256.848)] [--fve-brand-text:oklch(27.8%_0.033_256.848)] [--fve-brand-ring:oklch(55.4%_0.046_257.417)]
        --fve-brand-subtle:oklch(27.8%_0.033_256.848) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-subtle:oklch(27.8%_0.033_256.848)] [&.dark]:[--fve-brand-subtle:oklch(27.8%_0.033_256.848)] data-[fve-color-mode=dark]:[--fve-brand-subtle:oklch(27.8%_0.033_256.848)] --fve-brand-solid:oklch(70.7%_0.022_261.325) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-solid:oklch(70.7%_0.022_261.325)] [&.dark]:[--fve-brand-solid:oklch(70.7%_0.022_261.325)] data-[fve-color-mode=dark]:[--fve-brand-solid:oklch(70.7%_0.022_261.325)] --fve-brand-hover:oklch(87.2%_0.01_258.338) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-hover:oklch(87.2%_0.01_258.338)] [&.dark]:[--fve-brand-hover:oklch(87.2%_0.01_258.338)] data-[fve-color-mode=dark]:[--fve-brand-hover:oklch(87.2%_0.01_258.338)] --fve-brand-active:oklch(92.8%_0.006_264.531) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-active:oklch(92.8%_0.006_264.531)] [&.dark]:[--fve-brand-active:oklch(92.8%_0.006_264.531)] data-[fve-color-mode=dark]:[--fve-brand-active:oklch(92.8%_0.006_264.531)] --fve-brand-text:oklch(96.7%_0.003_264.542) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-text:oklch(96.7%_0.003_264.542)] [&.dark]:[--fve-brand-text:oklch(96.7%_0.003_264.542)] data-[fve-color-mode=dark]:[--fve-brand-text:oklch(96.7%_0.003_264.542)] --fve-brand-ring:oklch(70.7%_0.022_261.325) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-ring:oklch(70.7%_0.022_261.325)] [&.dark]:[--fve-brand-ring:oklch(70.7%_0.022_261.325)] data-[fve-color-mode=dark]:[--fve-brand-ring:oklch(70.7%_0.022_261.325)]
    """

/// <summary>
/// Shared announcement policy for persistent notices and dismissible notifications.
/// </summary>
/// <category>accessibility</category>
[<RequireQualifiedAccess>]
type LiveAnnouncement =
    | Static
    | Polite
    | Assertive

/// <category>theming</category>
[<RequireQualifiedAccess>]
type ControlSize =
    | Small
    | Medium
    | Large

/// <category>theming</category>
[<RequireQualifiedAccess>]
module ControlSize =
    /// Apply to a region to size its controls independently of layout density.
    let className = function
        | ControlSize.Small -> "fve-control-small [--fve-control-min-height:2rem] [--fve-control-padding-block:0.375rem] [--fve-control-font-size:0.875rem] [--fve-control-line-height:1.25rem]"
        | ControlSize.Medium -> "fve-control-medium [--fve-control-min-height:2.5rem] [--fve-control-padding-block:0.5rem] [--fve-control-font-size:1rem] [--fve-control-line-height:1.5rem]"
        | ControlSize.Large -> "fve-control-large [--fve-control-min-height:3rem] [--fve-control-padding-block:0.75rem] [--fve-control-font-size:1rem] [--fve-control-line-height:1.5rem]"

/// <category>theming</category>
[<RequireQualifiedAccess>]
type Radius =
    | None
    | Medium
    | Large
    | Full

/// <category>theming</category>
[<RequireQualifiedAccess>]
type Density =
    | Compact
    | Comfortable

/// <category>theming</category>
[<NoEquality; NoComparison>]
type ComponentsTheme =
    private
        { paletteClass:string
          radiusClass:string
          densityClass:string
          controlSizeClass:string }

/// <category>theming</category>
[<RequireQualifiedAccess>]
module ComponentsTheme =
    let private comfortableDensity = "fve-density-comfortable [--fve-navigation-padding-block:0.5rem] [--fve-navigation-min-height:2.25rem] [--fve-shell-bar-min-height:4rem]"

    let sky =
        { paletteClass = ThemeClasses.sky
          radiusClass = "fve-radius-large"
          densityClass = comfortableDensity
          controlSizeClass = ControlSize.className ControlSize.Medium }

    let emerald =
        { paletteClass = ThemeClasses.emerald
          radiusClass = "fve-radius-large"
          densityClass = comfortableDensity
          controlSizeClass = ControlSize.className ControlSize.Medium }

    let amber =
        { paletteClass = ThemeClasses.amber
          radiusClass = "fve-radius-large"
          densityClass = comfortableDensity
          controlSizeClass = ControlSize.className ControlSize.Medium }

    let cyan =
        { paletteClass = ThemeClasses.cyan
          radiusClass = "fve-radius-large"
          densityClass = comfortableDensity
          controlSizeClass = ControlSize.className ControlSize.Medium }

    let neutral =
        { paletteClass = ThemeClasses.neutral
          radiusClass = "fve-radius-large"
          densityClass = comfortableDensity
          controlSizeClass = ControlSize.className ControlSize.Medium }

    let custom paletteClass =
        if String.IsNullOrWhiteSpace paletteClass || Regex.IsMatch(paletteClass, "\\s") then
            invalidArg (nameof paletteClass) "A custom theme requires one non-empty CSS class."
        { paletteClass = paletteClass
          radiusClass = "fve-radius-large"
          densityClass = comfortableDensity
          controlSizeClass = ControlSize.className ControlSize.Medium }

    let withRadius radius theme =
        let radiusClass =
            match radius with
            | Radius.None -> "fve-radius-none [--fve-radius-control:0] [--fve-radius-panel:0]"
            | Radius.Medium -> "fve-radius-medium [--fve-radius-control:0.375rem] [--fve-radius-panel:0.5rem]"
            | Radius.Large -> "fve-radius-large [--fve-radius-control:0.5rem] [--fve-radius-panel:0.75rem]"
            | Radius.Full -> "fve-radius-full [--fve-radius-control:9999px] [--fve-radius-panel:1rem]"
        { theme with radiusClass = radiusClass }

    let withDensity density theme =
        let densityClass =
            match density with
            | Density.Compact -> "fve-density-compact [--fve-navigation-padding-block:0.25rem] [--fve-navigation-min-height:1.75rem] [--fve-shell-bar-min-height:3rem]"
            | Density.Comfortable -> comfortableDensity
        { theme with densityClass = densityClass }

    let withControlSize size theme =
        { theme with controlSizeClass = ControlSize.className size }

    let className theme =
        [ ThemeClasses.foundation; theme.paletteClass; theme.radiusClass; theme.densityClass; theme.controlSizeClass ]
        |> String.concat " "

    let attributes theme =
        [ _class (className theme) ]

/// <summary>
/// Explicit CSS colors for one mode. Check contrast for every foreground/background pair.
/// </summary>
/// <category>theming</category>
type ColorPaletteMode =
    { solid:string
      solidHover:string
      solidActive:string
      onSolid:string
      soft:string
      softHover:string
      softActive:string
      text:string
      border:string
      focus:string }

/// <summary>
/// A custom palette supplies both modes; component-owned Color unions select it with Custom.
/// </summary>
/// <category>theming</category>
type ColorPalette =
    { light:ColorPaletteMode
      dark:ColorPaletteMode }

module internal ComponentColors =
    let private mode solid onSolid soft mix =
        { solid = solid
          solidHover = $"color-mix(in srgb, {solid} 85%%, {mix})"
          solidActive = $"color-mix(in srgb, {solid} 70%%, {mix})"
          onSolid = onSolid
          soft = soft
          softHover = $"color-mix(in srgb, {soft} 90%%, {solid})"
          softActive = $"color-mix(in srgb, {soft} 80%%, {solid})"
          text = solid
          border = solid
          focus = solid }

    // Defaults are contrast-aware pairs, not arbitrary user colors. Each token can be
    // overridden independently by a consumer's theme; Custom requires explicit pairs.
    let private tokens prefix light dark =
        let apply (value:ColorPaletteMode) =
            let token name fallback = $"var(--fve-{prefix}-{name}, {fallback})"
            { solid = token "solid" value.solid
              solidHover = token "hover" value.solidHover
              solidActive = token "active" value.solidActive
              onSolid = token "on-solid" value.onSolid
              soft = token "soft" value.soft
              softHover = token "soft-hover" value.softHover
              softActive = token "soft-active" value.softActive
              text = token "text" value.text
              border = token "border" value.border
              focus = token "focus" value.focus }
        { light = apply light; dark = apply dark }

    let primary = tokens "primary" (mode "var(--fve-brand-text)" "#ffffff" "var(--fve-brand-subtle)" "#000000") (mode "var(--fve-brand-text)" "#101828" "var(--fve-brand-subtle)" "#ffffff")
    let secondary = tokens "secondary" (mode "#6d28d9" "#ffffff" "#f5f3ff" "#000000") (mode "#c4b5fd" "#101828" "#2e1065" "#ffffff")
    let success = tokens "success" (mode "#15803d" "#ffffff" "#f0fdf4" "#000000") (mode "#86efac" "#101828" "#052e16" "#ffffff")
    let warning = tokens "warning" (mode "#92400e" "#ffffff" "#fffbeb" "#000000") (mode "#fde68a" "#101828" "#451a03" "#ffffff")
    let error = tokens "error" (mode "#b91c1c" "#ffffff" "#fef2f2" "#000000") (mode "#fca5a5" "#101828" "#450a0a" "#ffffff")
    let info = tokens "info" (mode "#1d4ed8" "#ffffff" "#eff6ff" "#000000") (mode "#93c5fd" "#101828" "#172554" "#ffffff")
    let neutral = tokens "neutral" (mode "#374151" "#ffffff" "var(--fve-surface-subtle)" "#000000") (mode "#e5e7eb" "#101828" "var(--fve-surface-subtle)" "#ffffff")

    let style (palette:ColorPalette) (attributes:HtmlAttribute list) =
        let values (colors:ColorPaletteMode) =
            [ "solid", colors.solid; "hover", colors.solidHover; "active", colors.solidActive
              "on-solid", colors.onSolid; "soft", colors.soft; "soft-hover", colors.softHover
              "soft-active", colors.softActive; "text", colors.text; "border", colors.border; "focus", colors.focus ]
        let consumerStyle =
            attributes
            |> List.choose (fun attribute ->
                if String.Equals(attribute.Name, "style", StringComparison.OrdinalIgnoreCase) then
                    attribute.Value |> ValueOption.toOption
                else None)
        let colors =
            List.zip (values palette.light) (values palette.dark)
            |> List.map (fun ((name, light), (_, dark)) -> $"--fve-color-{name}:light-dark({light},{dark})")
        String.concat ";" (consumerStyle @ colors)

    let solid = "bg-[var(--fve-color-solid)] text-[var(--fve-color-on-solid)]"
    let soft = "bg-[var(--fve-color-soft)] text-[var(--fve-color-text)]"
    let outline = "bg-transparent text-[var(--fve-color-text)] ring-1 ring-inset ring-[color-mix(in_srgb,var(--fve-color-border)_20%,transparent)]"
    let ghost = "bg-transparent text-[var(--fve-color-text)]"
    let solidInteraction = "enabled:hover:bg-[var(--fve-color-hover)] enabled:active:bg-[var(--fve-color-active)]"
    let softInteraction = "enabled:hover:bg-[var(--fve-color-soft-hover)] enabled:active:bg-[var(--fve-color-soft-active)]"
    let focus = "focus-visible:ring-[var(--fve-color-focus)] focus-visible:ring-offset-[var(--fve-surface)]"
module internal ComponentHtml =
    let classes values = values |> List.filter (String.IsNullOrWhiteSpace >> not) |> String.concat " "

    let horizontalScrollClasses = "min-w-0 max-w-full overflow-x-auto p-1 scroll-p-1 outline-none focus-visible:outline-2 focus-visible:outline-solid focus-visible:-outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] forced-colors:focus-visible:outline-[Highlight]"

    let horizontalScrollAttributes =
        [ _tabindex 0
          _dataOn ("focusin", "if (evt.target !== el && !evt.target.closest('[role=menu]')) evt.target.scrollIntoView({block: 'nearest', inline: 'nearest'})")
          _dataOn ("keydown", "if (evt.target === el && !evt.altKey && !evt.ctrlKey && !evt.metaKey && !evt.shiftKey && (evt.key === 'ArrowLeft' || evt.key === 'ArrowRight')) { evt.preventDefault(); el.scrollBy({left: evt.key === 'ArrowLeft' ? -80 : 80}) }") ]

    let popupClasses = "outline-none forced-colors:border forced-colors:border-[CanvasText]"
    let popupFieldClasses = "outline-none focus-visible:outline-solid focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] forced-colors:focus-visible:outline-solid forced-colors:focus-visible:outline-[Highlight]"
    let popupControlClasses = "outline-none focus-visible:outline-solid focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-[var(--fve-brand-ring)] forced-colors:focus-visible:outline-solid forced-colors:focus-visible:outline-[Highlight]"
    let popupItemClasses =
        classes [
            "outline-none aria-selected:bg-[var(--fve-brand-subtle)] aria-selected:text-[var(--fve-brand-text)] aria-selected:font-semibold"
            "not-disabled:not-aria-disabled:focus:bg-[var(--fve-popup-active-background)] not-disabled:not-aria-disabled:focus:text-[var(--fve-popup-active-text)] not-disabled:not-aria-disabled:data-[active=true]:bg-[var(--fve-popup-active-background)] not-disabled:not-aria-disabled:data-[active=true]:text-[var(--fve-popup-active-text)]"
            "aria-selected:focus:bg-[var(--fve-popup-active-background)] aria-selected:focus:text-[var(--fve-popup-active-text)] aria-selected:data-[active=true]:bg-[var(--fve-popup-active-background)] aria-selected:data-[active=true]:text-[var(--fve-popup-active-text)]"
            "forced-colors:focus:outline-solid forced-colors:focus:outline-2 forced-colors:focus:-outline-offset-2 forced-colors:focus:outline-[Highlight] forced-colors:data-[active=true]:outline-solid forced-colors:data-[active=true]:outline-2 forced-colors:data-[active=true]:-outline-offset-2 forced-colors:data-[active=true]:outline-[Highlight]" ]

    let safeAttributes reservedNames attributes =
        attributes
        |> List.filter (fun attribute ->
            reservedNames
            |> List.exists (fun reservedName ->
                String.Equals(attribute.Name, reservedName, StringComparison.OrdinalIgnoreCase)
                || (reservedName.EndsWith(':') && attribute.Name.StartsWith(reservedName, StringComparison.OrdinalIgnoreCase)))
            |> not)

    let javascriptString value = System.Text.Json.JsonSerializer.Serialize(value)

    let signalToken value =
        let token = Regex.Replace(value, "[^A-Za-z0-9]+", "_").Trim('_')
        if String.IsNullOrEmpty token then "component" else token.ToLowerInvariant()

    let optionToken (value:string) =
        value
        |> Encoding.UTF8.GetBytes
        |> Array.map (fun character -> character.ToString("x2"))
        |> String.concat ""
        |> (+) "v"

    let controlSizeClass size = size |> Option.map ControlSize.className |> Option.defaultValue ""

    let sizeClasses size =
        classes [ controlSizeClass size; "min-h-[var(--fve-control-min-height)] px-3 py-[var(--fve-control-padding-block)] text-[length:var(--fve-control-font-size)] leading-[var(--fve-control-line-height)]" ]

    let iconButtonSizeClasses size =
        classes [ controlSizeClass size; "size-[var(--fve-control-min-height)] p-0" ]

    let loadingGlyph size =
        let sizeClass =
            match size with
            | ControlSize.Small -> "size-3.5"
            | ControlSize.Medium -> "size-4"
            | ControlSize.Large -> "size-5"
        span {
            _ariaHidden "true"
            _class (classes [ "shrink-0 animate-spin rounded-full border-2 border-current border-r-transparent motion-reduce:animate-none"; sizeClass ])
        }
