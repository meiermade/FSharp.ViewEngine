import { rocket } from './datastar-rocket.1.0.5.js';
import { enhanceNativeControl } from './native-control.js';

rocket('fve-checkbox', { mode: 'light', setup: context => enhanceNativeControl(context, [], true) });
