import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { MatIconModule } from '@angular/material/icon';
import { SetupWizardService, WizardStep } from '../../services/setup-wizard.service';
import { WelcomeComponent } from '../welcome/welcome.component';
import { BlePairingComponent } from '../ble-pairing/ble-pairing.component';
import { WifiSetupComponent } from '../wifi-setup/wifi-setup.component';
import { NodeInfoComponent } from '../node-info/node-info.component';
import { FirstSensorComponent } from '../first-sensor/first-sensor.component';
import { SuccessComponent } from '../success/success.component';

interface StepInfo {
  step: WizardStep;
  label: string;
  icon: string;
}

@Component({
  selector: 'myiotgrid-setup-wizard',
  standalone: true,
  imports: [
    CommonModule,
    MatProgressBarModule,
    MatStepperModule,
    MatIconModule,
    WelcomeComponent,
    BlePairingComponent,
    WifiSetupComponent,
    NodeInfoComponent,
    FirstSensorComponent,
    SuccessComponent
  ],
  templateUrl: './setup-wizard.component.html',
  styleUrl: './setup-wizard.component.scss'
})
export class SetupWizardComponent {
  private readonly wizardService = inject(SetupWizardService);

  readonly currentStep = this.wizardService.currentStep;
  readonly progress = this.wizardService.progress;
  readonly currentStepIndex = this.wizardService.currentStepIndex;

  readonly steps: StepInfo[] = [
    { step: 'welcome', label: 'Start', icon: 'play_circle' },
    { step: 'ble-pairing', label: 'BLE', icon: 'bluetooth' },
    { step: 'wifi-setup', label: 'WiFi', icon: 'wifi' },
    { step: 'node-info', label: 'Info', icon: 'settings' },
    { step: 'first-sensor', label: 'Sensor', icon: 'sensors' },
    { step: 'success', label: 'Fertig', icon: 'check_circle' }
  ];

  isStepCompleted(step: WizardStep): boolean {
    const currentIndex = this.currentStepIndex();
    const stepIndex = this.steps.findIndex(s => s.step === step);
    return stepIndex < currentIndex;
  }

  isStepActive(step: WizardStep): boolean {
    return this.currentStep() === step;
  }
}
