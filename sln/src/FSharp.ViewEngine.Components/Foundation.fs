namespace FSharp.ViewEngine.Components.Primitives

open System
open System.Text
open System.Text.RegularExpressions
open FSharp.ViewEngine
open type Html
open type Datastar

module private ThemeClasses =
    let private normalize (value:string) = Regex.Replace(value, "\\s+", " ").Trim()

    let foundation = normalize """
        fve-components
        [--fve-page:oklch(98.5%_0.002_247.839)] [--fve-surface:oklch(100%_0_0)] [--fve-surface-subtle:oklch(96.7%_0.003_264.542)] [--fve-surface-hover:oklch(96.7%_0.003_264.542)] [--fve-surface-active:oklch(92.8%_0.006_264.531)]
        [--fve-text:oklch(21%_0.034_264.665)] [--fve-muted-text:oklch(44.6%_0.03_256.802)] [--fve-border:oklch(87.2%_0.01_258.338)] [--fve-overlay-backdrop:oklch(13%_0.028_261.692/55%)]
        [--fve-neutral-subtle:oklch(96.7%_0.003_264.542)] [--fve-neutral-text:oklch(37.3%_0.034_259.733)] [--fve-neutral-ring:oklch(70.7%_0.022_261.325)]
        [--fve-positive-subtle:oklch(96.2%_0.044_156.743)] [--fve-positive-text:oklch(44.8%_0.119_151.328)] [--fve-positive-ring:oklch(72.3%_0.219_149.579)]
        [--fve-warning-subtle:oklch(97.3%_0.071_103.193)] [--fve-warning-text:oklch(47.6%_0.114_61.907)] [--fve-warning-ring:oklch(76.9%_0.188_70.08)]
        [--fve-critical-subtle:oklch(97.1%_0.013_17.38)] [--fve-critical-solid:oklch(57.7%_0.245_27.325)] [--fve-critical-hover:oklch(50.5%_0.213_27.518)] [--fve-critical-active:oklch(44.4%_0.177_26.899)] [--fve-critical-text:oklch(44.4%_0.177_26.899)] [--fve-critical-ring:oklch(63.7%_0.237_25.331)]
        [--fve-info-subtle:oklch(97%_0.014_254.604)] [--fve-info-text:oklch(48.8%_0.243_264.376)] [--fve-info-ring:oklch(62.3%_0.214_259.815)]
        [--fve-radius-control:0.5rem] [--fve-radius-panel:0.75rem] [--fve-control-padding-block:0.5rem] [--fve-control-min-height:2.5rem]
        [--fve-navigation-padding-block:0.5rem] [--fve-navigation-min-height:2.25rem] [--fve-shell-bar-min-height:4rem]
        [--fve-popup-active-background:var(--fve-brand-solid)] [--fve-popup-active-text:white]
        dark:not-data-[fve-color-mode=light]:[--fve-popup-active-background:var(--fve-brand-text)] dark:not-data-[fve-color-mode=light]:[--fve-popup-active-text:var(--fve-surface)]
        [.dark_&:not([data-fve-color-mode=light])]:[--fve-popup-active-background:var(--fve-brand-text)] [.dark_&:not([data-fve-color-mode=light])]:[--fve-popup-active-text:var(--fve-surface)]
        [&.dark]:[--fve-popup-active-background:var(--fve-brand-text)] [&.dark]:[--fve-popup-active-text:var(--fve-surface)]
        data-[fve-color-mode=dark]:[--fve-popup-active-background:var(--fve-brand-text)] data-[fve-color-mode=dark]:[--fve-popup-active-text:var(--fve-surface)]
        dark:not-data-[fve-color-mode=light]:[--fve-page:oklch(13%_0.028_261.692)] [.dark_&:not([data-fve-color-mode=light])]:[--fve-page:oklch(13%_0.028_261.692)] [&.dark]:[--fve-page:oklch(13%_0.028_261.692)] data-[fve-color-mode=dark]:[--fve-page:oklch(13%_0.028_261.692)]
        --fve-surface:oklch(21%_0.034_264.665) [.dark_&:not([data-fve-color-mode=light])]:[--fve-surface:oklch(21%_0.034_264.665)] [&.dark]:[--fve-surface:oklch(21%_0.034_264.665)] data-[fve-color-mode=dark]:[--fve-surface:oklch(21%_0.034_264.665)]
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
        fve-theme-sky [--fve-brand-subtle:oklch(97.7%_0.013_236.62)] [--fve-brand-solid:oklch(50%_0.134_242.749)] [--fve-brand-hover:oklch(44.3%_0.11_240.79)] [--fve-brand-active:oklch(39.1%_0.09_240.876)] [--fve-brand-text:oklch(44.3%_0.11_240.79)] [--fve-brand-ring:oklch(68.5%_0.169_237.323)]
        --fve-brand-subtle:oklch(29.3%_0.066_243.157) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-subtle:oklch(29.3%_0.066_243.157)] [&.dark]:[--fve-brand-subtle:oklch(29.3%_0.066_243.157)] data-[fve-color-mode=dark]:[--fve-brand-subtle:oklch(29.3%_0.066_243.157)] --fve-brand-solid:oklch(55.4%_0.135_241.966) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-solid:oklch(55.4%_0.135_241.966)] [&.dark]:[--fve-brand-solid:oklch(55.4%_0.135_241.966)] data-[fve-color-mode=dark]:[--fve-brand-solid:oklch(55.4%_0.135_241.966)] --fve-brand-hover:oklch(65.5%_0.15_237.323) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-hover:oklch(65.5%_0.15_237.323)] [&.dark]:[--fve-brand-hover:oklch(65.5%_0.15_237.323)] data-[fve-color-mode=dark]:[--fve-brand-hover:oklch(65.5%_0.15_237.323)] --fve-brand-active:oklch(74.6%_0.16_232.661) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-active:oklch(74.6%_0.16_232.661)] [&.dark]:[--fve-brand-active:oklch(74.6%_0.16_232.661)] data-[fve-color-mode=dark]:[--fve-brand-active:oklch(74.6%_0.16_232.661)] --fve-brand-text:oklch(82.8%_0.111_230.318) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-text:oklch(82.8%_0.111_230.318)] [&.dark]:[--fve-brand-text:oklch(82.8%_0.111_230.318)] data-[fve-color-mode=dark]:[--fve-brand-text:oklch(82.8%_0.111_230.318)] --fve-brand-ring:oklch(74.6%_0.16_232.661) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-ring:oklch(74.6%_0.16_232.661)] [&.dark]:[--fve-brand-ring:oklch(74.6%_0.16_232.661)] data-[fve-color-mode=dark]:[--fve-brand-ring:oklch(74.6%_0.16_232.661)]
    """

    let emerald = normalize """
        fve-theme-emerald [--fve-brand-subtle:oklch(97.9%_0.021_166.113)] [--fve-brand-solid:oklch(59.6%_0.145_163.225)] [--fve-brand-hover:oklch(50.8%_0.118_165.612)] [--fve-brand-active:oklch(43.2%_0.095_166.913)] [--fve-brand-text:oklch(43.2%_0.095_166.913)] [--fve-brand-ring:oklch(69.6%_0.17_162.48)]
        --fve-brand-subtle:oklch(26.2%_0.051_172.552) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-subtle:oklch(26.2%_0.051_172.552)] [&.dark]:[--fve-brand-subtle:oklch(26.2%_0.051_172.552)] data-[fve-color-mode=dark]:[--fve-brand-subtle:oklch(26.2%_0.051_172.552)] --fve-brand-solid:oklch(56.5%_0.128_163.225) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-solid:oklch(56.5%_0.128_163.225)] [&.dark]:[--fve-brand-solid:oklch(56.5%_0.128_163.225)] data-[fve-color-mode=dark]:[--fve-brand-solid:oklch(56.5%_0.128_163.225)] --fve-brand-hover:oklch(66.5%_0.154_162.48) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-hover:oklch(66.5%_0.154_162.48)] [&.dark]:[--fve-brand-hover:oklch(66.5%_0.154_162.48)] data-[fve-color-mode=dark]:[--fve-brand-hover:oklch(66.5%_0.154_162.48)] --fve-brand-active:oklch(76.5%_0.177_163.223) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-active:oklch(76.5%_0.177_163.223)] [&.dark]:[--fve-brand-active:oklch(76.5%_0.177_163.223)] data-[fve-color-mode=dark]:[--fve-brand-active:oklch(76.5%_0.177_163.223)] --fve-brand-text:oklch(84.5%_0.143_164.978) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-text:oklch(84.5%_0.143_164.978)] [&.dark]:[--fve-brand-text:oklch(84.5%_0.143_164.978)] data-[fve-color-mode=dark]:[--fve-brand-text:oklch(84.5%_0.143_164.978)] --fve-brand-ring:oklch(76.5%_0.177_163.223) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-ring:oklch(76.5%_0.177_163.223)] [&.dark]:[--fve-brand-ring:oklch(76.5%_0.177_163.223)] data-[fve-color-mode=dark]:[--fve-brand-ring:oklch(76.5%_0.177_163.223)]
    """

    let amber = normalize """
        fve-theme-amber [--fve-brand-subtle:oklch(98.7%_0.022_95.277)] [--fve-brand-solid:oklch(66.6%_0.179_58.318)] [--fve-brand-hover:oklch(55.5%_0.163_48.998)] [--fve-brand-active:oklch(47.3%_0.137_46.201)] [--fve-brand-text:oklch(47.3%_0.137_46.201)] [--fve-brand-ring:oklch(76.9%_0.188_70.08)]
        --fve-brand-subtle:oklch(27.9%_0.077_45.635) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-subtle:oklch(27.9%_0.077_45.635)] [&.dark]:[--fve-brand-subtle:oklch(27.9%_0.077_45.635)] data-[fve-color-mode=dark]:[--fve-brand-subtle:oklch(27.9%_0.077_45.635)] --fve-brand-solid:oklch(61%_0.17_55.5) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-solid:oklch(61%_0.17_55.5)] [&.dark]:[--fve-brand-solid:oklch(61%_0.17_55.5)] data-[fve-color-mode=dark]:[--fve-brand-solid:oklch(61%_0.17_55.5)] --fve-brand-hover:oklch(70%_0.18_65) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-hover:oklch(70%_0.18_65)] [&.dark]:[--fve-brand-hover:oklch(70%_0.18_65)] data-[fve-color-mode=dark]:[--fve-brand-hover:oklch(70%_0.18_65)] --fve-brand-active:oklch(80%_0.17_75) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-active:oklch(80%_0.17_75)] [&.dark]:[--fve-brand-active:oklch(80%_0.17_75)] data-[fve-color-mode=dark]:[--fve-brand-active:oklch(80%_0.17_75)] --fve-brand-text:oklch(87.9%_0.169_91.605) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-text:oklch(87.9%_0.169_91.605)] [&.dark]:[--fve-brand-text:oklch(87.9%_0.169_91.605)] data-[fve-color-mode=dark]:[--fve-brand-text:oklch(87.9%_0.169_91.605)] --fve-brand-ring:oklch(76.9%_0.188_70.08) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-ring:oklch(76.9%_0.188_70.08)] [&.dark]:[--fve-brand-ring:oklch(76.9%_0.188_70.08)] data-[fve-color-mode=dark]:[--fve-brand-ring:oklch(76.9%_0.188_70.08)]
    """

    let cyan = normalize """
        fve-theme-cyan [--fve-brand-subtle:oklch(98.4%_0.019_200.873)] [--fve-brand-solid:oklch(60.9%_0.126_221.723)] [--fve-brand-hover:oklch(52%_0.105_223.128)] [--fve-brand-active:oklch(45%_0.085_224.283)] [--fve-brand-text:oklch(45%_0.085_224.283)] [--fve-brand-ring:oklch(71.5%_0.143_215.221)]
        --fve-brand-subtle:oklch(30.2%_0.056_229.695) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-subtle:oklch(30.2%_0.056_229.695)] [&.dark]:[--fve-brand-subtle:oklch(30.2%_0.056_229.695)] data-[fve-color-mode=dark]:[--fve-brand-subtle:oklch(30.2%_0.056_229.695)] --fve-brand-solid:oklch(60.9%_0.126_221.723) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-solid:oklch(60.9%_0.126_221.723)] [&.dark]:[--fve-brand-solid:oklch(60.9%_0.126_221.723)] data-[fve-color-mode=dark]:[--fve-brand-solid:oklch(60.9%_0.126_221.723)] --fve-brand-hover:oklch(71.5%_0.143_215.221) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-hover:oklch(71.5%_0.143_215.221)] [&.dark]:[--fve-brand-hover:oklch(71.5%_0.143_215.221)] data-[fve-color-mode=dark]:[--fve-brand-hover:oklch(71.5%_0.143_215.221)] --fve-brand-active:oklch(78.9%_0.154_211.53) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-active:oklch(78.9%_0.154_211.53)] [&.dark]:[--fve-brand-active:oklch(78.9%_0.154_211.53)] data-[fve-color-mode=dark]:[--fve-brand-active:oklch(78.9%_0.154_211.53)] --fve-brand-text:oklch(86.5%_0.127_207.078) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-text:oklch(86.5%_0.127_207.078)] [&.dark]:[--fve-brand-text:oklch(86.5%_0.127_207.078)] data-[fve-color-mode=dark]:[--fve-brand-text:oklch(86.5%_0.127_207.078)] --fve-brand-ring:oklch(71.5%_0.143_215.221) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-ring:oklch(71.5%_0.143_215.221)] [&.dark]:[--fve-brand-ring:oklch(71.5%_0.143_215.221)] data-[fve-color-mode=dark]:[--fve-brand-ring:oklch(71.5%_0.143_215.221)]
    """

    let neutral = normalize """
        fve-theme-neutral [--fve-brand-subtle:oklch(96.7%_0.003_264.542)] [--fve-brand-solid:oklch(44.6%_0.03_256.802)] [--fve-brand-hover:oklch(37.3%_0.034_259.733)] [--fve-brand-active:oklch(27.8%_0.033_256.848)] [--fve-brand-text:oklch(27.8%_0.033_256.848)] [--fve-brand-ring:oklch(70.7%_0.022_261.325)]
        --fve-brand-subtle:oklch(27.8%_0.033_256.848) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-subtle:oklch(27.8%_0.033_256.848)] [&.dark]:[--fve-brand-subtle:oklch(27.8%_0.033_256.848)] data-[fve-color-mode=dark]:[--fve-brand-subtle:oklch(27.8%_0.033_256.848)] --fve-brand-solid:oklch(70.7%_0.022_261.325) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-solid:oklch(70.7%_0.022_261.325)] [&.dark]:[--fve-brand-solid:oklch(70.7%_0.022_261.325)] data-[fve-color-mode=dark]:[--fve-brand-solid:oklch(70.7%_0.022_261.325)] --fve-brand-hover:oklch(87.2%_0.01_258.338) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-hover:oklch(87.2%_0.01_258.338)] [&.dark]:[--fve-brand-hover:oklch(87.2%_0.01_258.338)] data-[fve-color-mode=dark]:[--fve-brand-hover:oklch(87.2%_0.01_258.338)] --fve-brand-active:oklch(92.8%_0.006_264.531) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-active:oklch(92.8%_0.006_264.531)] [&.dark]:[--fve-brand-active:oklch(92.8%_0.006_264.531)] data-[fve-color-mode=dark]:[--fve-brand-active:oklch(92.8%_0.006_264.531)] --fve-brand-text:oklch(96.7%_0.003_264.542) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-text:oklch(96.7%_0.003_264.542)] [&.dark]:[--fve-brand-text:oklch(96.7%_0.003_264.542)] data-[fve-color-mode=dark]:[--fve-brand-text:oklch(96.7%_0.003_264.542)] --fve-brand-ring:oklch(70.7%_0.022_261.325) [.dark_&:not([data-fve-color-mode=light])]:[--fve-brand-ring:oklch(70.7%_0.022_261.325)] [&.dark]:[--fve-brand-ring:oklch(70.7%_0.022_261.325)] data-[fve-color-mode=dark]:[--fve-brand-ring:oklch(70.7%_0.022_261.325)]
    """

[<RequireQualifiedAccess>]
type Tone =
    | Neutral
    | Brand
    | Positive
    | Warning
    | Critical
    | Informative

[<RequireQualifiedAccess>]
type ControlSize =
    | Small
    | Medium
    | Large

[<RequireQualifiedAccess>]
module ControlSize =
    /// Apply to a region to size its controls independently of layout density.
    let className = function
        | ControlSize.Small -> "fve-control-small [--fve-control-min-height:2rem] [--fve-control-padding-block:0.375rem] [--fve-control-font-size:0.875rem] [--fve-control-line-height:1.25rem]"
        | ControlSize.Medium -> "fve-control-medium [--fve-control-min-height:2.5rem] [--fve-control-padding-block:0.5rem] [--fve-control-font-size:1rem] [--fve-control-line-height:1.5rem]"
        | ControlSize.Large -> "fve-control-large [--fve-control-min-height:3rem] [--fve-control-padding-block:0.75rem] [--fve-control-font-size:1rem] [--fve-control-line-height:1.5rem]"

[<RequireQualifiedAccess>]
type Radius =
    | None
    | Medium
    | Large
    | Full

[<RequireQualifiedAccess>]
type Density =
    | Compact
    | Comfortable

[<NoEquality; NoComparison>]
type ComponentsTheme =
    private
        { paletteClass:string
          radiusClass:string
          densityClass:string
          controlSizeClass:string }

[<RequireQualifiedAccess>]
module ComponentsTheme =
    let sky =
        { paletteClass = ThemeClasses.sky
          radiusClass = "fve-radius-large"
          densityClass = "fve-density-comfortable"
          controlSizeClass = ControlSize.className ControlSize.Medium }

    let emerald =
        { paletteClass = ThemeClasses.emerald
          radiusClass = "fve-radius-large"
          densityClass = "fve-density-comfortable"
          controlSizeClass = ControlSize.className ControlSize.Medium }

    let amber =
        { paletteClass = ThemeClasses.amber
          radiusClass = "fve-radius-large"
          densityClass = "fve-density-comfortable"
          controlSizeClass = ControlSize.className ControlSize.Medium }

    let cyan =
        { paletteClass = ThemeClasses.cyan
          radiusClass = "fve-radius-large"
          densityClass = "fve-density-comfortable"
          controlSizeClass = ControlSize.className ControlSize.Medium }

    let neutral =
        { paletteClass = ThemeClasses.neutral
          radiusClass = "fve-radius-large"
          densityClass = "fve-density-comfortable"
          controlSizeClass = ControlSize.className ControlSize.Medium }

    let custom paletteClass =
        if String.IsNullOrWhiteSpace paletteClass || Regex.IsMatch(paletteClass, "\\s") then
            invalidArg (nameof paletteClass) "A custom theme requires one non-empty CSS class."
        { paletteClass = paletteClass
          radiusClass = "fve-radius-large"
          densityClass = "fve-density-comfortable"
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
            | Density.Compact -> "fve-density-compact [--fve-navigation-padding-block:0.375rem] [--fve-navigation-min-height:2rem] [--fve-shell-bar-min-height:3rem]"
            | Density.Comfortable -> "fve-density-comfortable [--fve-navigation-padding-block:0.5rem] [--fve-navigation-min-height:2.25rem] [--fve-shell-bar-min-height:4rem]"
        { theme with densityClass = densityClass }

    let withControlSize size theme =
        { theme with controlSizeClass = ControlSize.className size }

    let className theme =
        [ ThemeClasses.foundation; theme.paletteClass; theme.radiusClass; theme.densityClass; theme.controlSizeClass ]
        |> String.concat " "

    let attributes theme =
        [ _class (className theme) ]

module internal ComponentHtml =
    let classes values = values |> List.filter (String.IsNullOrWhiteSpace >> not) |> String.concat " "

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

    let toneClasses = function
        | Tone.Neutral -> "bg-[var(--fve-neutral-subtle)] text-[var(--fve-neutral-text)] ring-[var(--fve-border)]"
        | Tone.Brand -> "bg-[var(--fve-brand-subtle)] text-[var(--fve-brand-text)] ring-[var(--fve-brand-ring)]"
        | Tone.Positive -> "bg-[var(--fve-positive-subtle)] text-[var(--fve-positive-text)] ring-[var(--fve-positive-ring)]"
        | Tone.Warning -> "bg-[var(--fve-warning-subtle)] text-[var(--fve-warning-text)] ring-[var(--fve-warning-ring)]"
        | Tone.Critical -> "bg-[var(--fve-critical-subtle)] text-[var(--fve-critical-text)] ring-[var(--fve-critical-ring)]"
        | Tone.Informative -> "bg-[var(--fve-info-subtle)] text-[var(--fve-info-text)] ring-[var(--fve-info-ring)]"

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
            _class (classes [ "shrink-0 animate-spin rounded-full border-2 border-[var(--fve-border)] border-t-[var(--fve-brand-solid)] motion-reduce:animate-none"; sizeClass ])
        }
