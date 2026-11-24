import { rocket } from './datastar-rocket.1.0.5.js';
import { enhanceOverlay } from './native-overlay.js';

rocket('fve-dialog', { mode: 'light', scope: false, setup: enhanceOverlay });
