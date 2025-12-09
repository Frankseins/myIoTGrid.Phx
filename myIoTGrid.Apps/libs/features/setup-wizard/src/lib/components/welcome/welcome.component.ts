import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDividerModule } from '@angular/material/divider';
import { SetupWizardService } from '../../services/setup-wizard.service';

interface ChecklistItem {
  id: string;
  label: string;
  description: string;
  icon: string;
  checked: boolean;
}

@Component({
  selector: 'myiotgrid-setup-welcome',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatDividerModule
  ],
  templateUrl: './welcome.component.html',
  styleUrl: './welcome.component.scss'
})
export class WelcomeComponent implements OnInit {
  private readonly wizardService = inject(SetupWizardService);

  readonly checklistItems = signal<ChecklistItem[]>([
    {
      id: 'node-ready',
      label: 'Node ist eingeschaltet',
      description: 'Der ESP32/LoRa32 ist mit Strom versorgt und die LED blinkt',
      icon: 'power_settings_new',
      checked: false
    },
    {
      id: 'bluetooth-enabled',
      label: 'Bluetooth ist aktiviert',
      description: 'Bluetooth ist auf diesem Gerät eingeschaltet',
      icon: 'bluetooth',
      checked: false
    },
    {
      id: 'wifi-available',
      label: 'WiFi-Zugangsdaten bereit',
      description: 'SSID und Passwort Ihres WLAN-Netzwerks sind bekannt',
      icon: 'wifi',
      checked: false
    },
    {
      id: 'hub-online',
      label: 'Hub ist online',
      description: 'Der myIoTGrid Hub ist erreichbar und funktionsfähig',
      icon: 'cloud_done',
      checked: false
    }
  ]);

  readonly allChecked = signal(false);

  ngOnInit(): void {
    // Reset wizard state when entering welcome step
    this.wizardService.reset();
  }

  toggleItem(itemId: string): void {
    this.checklistItems.update(items =>
      items.map(item =>
        item.id === itemId ? { ...item, checked: !item.checked } : item
      )
    );
    this.updateAllChecked();
  }

  private updateAllChecked(): void {
    const items = this.checklistItems();
    this.allChecked.set(items.every(item => item.checked));
  }

  onStart(): void {
    this.wizardService.nextStep();
  }

  onCancel(): void {
    this.wizardService.exitWizard();
  }
}
