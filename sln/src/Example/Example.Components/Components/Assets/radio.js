import { rocket } from './datastar-rocket.1.0.4.js';
import { enhanceNativeControl } from './native-control.js';

rocket('fve-radio', { mode: 'light', scope: false, setup: context => enhanceNativeControl(context, ['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', ' ']) });
