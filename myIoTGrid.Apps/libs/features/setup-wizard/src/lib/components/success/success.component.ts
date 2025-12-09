import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { SetupWizardService } from '../../services/setup-wizard.service';

@Component({
  selector: 'myiotgrid-success',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatChipsModule
  ],
  templateUrl: './success.component.html',
  styleUrl: './success.component.scss'
})
export class SuccessComponent implements OnInit {
  private readonly wizardService = inject(SetupWizardService);

  readonly createdNode = this.wizardService.createdNode;
  readonly nodeInfo = this.wizardService.nodeInfo;
  readonly sensor = this.wizardService.sensor;
  readonly bleDevice = this.wizardService.bleDevice;
  readonly wifiCredentials = this.wizardService.wifiCredentials;

  readonly showConfetti = signal(true);

  ngOnInit(): void {
    // Trigger confetti animation
    this.launchConfetti();
  }

  private launchConfetti(): void {
    // Optional: Install canvas-confetti for celebration effect
    // npm install canvas-confetti @types/canvas-confetti
    // Then uncomment the code below:
    /*
    import('canvas-confetti').then(({ default: confetti }) => {
      const count = 200;
      const defaults = { origin: { y: 0.7 }, zIndex: 9999 };
      const fire = (particleRatio: number, opts: object) => {
        confetti({ ...defaults, ...opts, particleCount: Math.floor(count * particleRatio) });
      };
      fire(0.25, { spread: 26, startVelocity: 55 });
      fire(0.2, { spread: 60 });
      fire(0.35, { spread: 100, decay: 0.91, scalar: 0.8 });
      fire(0.1, { spread: 120, startVelocity: 25, decay: 0.92, scalar: 1.2 });
      fire(0.1, { spread: 120, startVelocity: 45 });
    }).catch(() => {});
    */
    console.log('Node setup complete!');
  }

  goToNode(): void {
    this.wizardService.goToCreatedNode();
  }

  goToNodeList(): void {
    this.wizardService.exitWizard();
  }

  addAnotherNode(): void {
    this.wizardService.reset();
  }
}
