import { rocket } from './datastar-rocket.1.0.4.js';
import { enhanceOverlay } from './native-overlay.js';

rocket('fve-drawer', { mode: 'light', scope: false, setup: enhanceOverlay });
