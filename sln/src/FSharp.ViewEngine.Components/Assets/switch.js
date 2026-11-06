import { rocket } from './datastar-rocket.1.0.4.js';
import { enhanceNativeControl } from './native-control.js';

rocket('fve-switch', { mode: 'light', scope: false, setup: enhanceNativeControl });
