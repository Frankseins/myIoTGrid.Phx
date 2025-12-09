import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { SetupWizardService } from '../../services/setup-wizard.service';
import { SensorApiService, NodeSensorAssignmentApiService } from '@myiotgrid/shared/data-access';
import { Sensor, CommunicationProtocol, CreateNodeSensorAssignmentDto } from '@myiotgrid/shared/models';

@Component({
  selector: 'myiotgrid-first-sensor',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './first-sensor.component.html',
  styleUrl: './first-sensor.component.scss'
})
export class FirstSensorComponent implements OnInit {
  private readonly wizardService = inject(SetupWizardService);
  private readonly sensorApiService = inject(SensorApiService);
  private readonly assignmentApiService = inject(NodeSensorAssignmentApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly nodeInfo = this.wizardService.nodeInfo;
  readonly isCreating = signal(false);
  readonly selectedSensors = signal<Sensor[]>([]);
  readonly existingSensors = signal<Sensor[]>([]);
  readonly isLoadingSensors = signal(false);

  readonly protocols = [
    { value: CommunicationProtocol.I2C, label: 'I²C' },
    { value: CommunicationProtocol.SPI, label: 'SPI' },
    { value: CommunicationProtocol.OneWire, label: '1-Wire' },
    { value: CommunicationProtocol.Analog, label: 'Analog' },
    { value: CommunicationProtocol.Digital, label: 'Digital' },
    { value: CommunicationProtocol.UltraSonic, label: 'Ultraschall' }
  ];

  ngOnInit(): void {
    this.loadExistingSensors();
  }

  private loadExistingSensors(): void {
    this.isLoadingSensors.set(true);
    this.sensorApiService.getAll().subscribe({
      next: (sensors) => {
        this.existingSensors.set(sensors);
        this.isLoadingSensors.set(false);
      },
      error: (error) => {
        console.error('Error loading sensors:', error);
        this.isLoadingSensors.set(false);
      }
    });
  }

  toggleSensorSelection(sensor: Sensor): void {
    const currentSelection = this.selectedSensors();
    const isSelected = currentSelection.some(s => s.id === sensor.id);

    if (isSelected) {
      // Remove from selection
      this.selectedSensors.set(currentSelection.filter(s => s.id !== sensor.id));
    } else {
      // Add to selection
      this.selectedSensors.set([...currentSelection, sensor]);
    }
  }

  isSensorSelected(sensor: Sensor): boolean {
    return this.selectedSensors().some(s => s.id === sensor.id);
  }

  async onComplete(): Promise<void> {
    const sensors = this.selectedSensors();

    if (sensors.length === 0) {
      this.snackBar.open('Bitte wählen Sie mindestens einen Sensor aus', 'Schließen', { duration: 3000 });
      return;
    }

    this.isCreating.set(true);

    try {
      // Node was already created in the node-info step
      const createdNode = this.wizardService.createdNode();

      if (!createdNode) {
        throw new Error('Node was not created - invalid wizard state');
      }

      // Assign all selected sensors to the existing node
      let endpointId = 1;
      for (const sensor of sensors) {
        const assignmentDto: CreateNodeSensorAssignmentDto = {
          sensorId: sensor.id,
          endpointId: endpointId++
        };

        try {
          await this.assignmentApiService.create(createdNode.id, assignmentDto).toPromise();
          console.log(`[FirstSensor] Sensor ${sensor.name} assigned to node (endpoint ${assignmentDto.endpointId})`);
        } catch (assignmentError) {
          console.warn(`[FirstSensor] Failed to assign sensor ${sensor.name}:`, assignmentError);
          // Continue with other sensors
        }
      }

      this.wizardService.completeWithExistingNode();
    } catch (error) {
      console.error('Error assigning sensors:', error);
      this.snackBar.open('Fehler beim Zuweisen der Sensoren', 'Schließen', { duration: 5000 });
      this.isCreating.set(false);
    }
  }

  onSkip(): void {
    this.wizardService.skipSensorStep();
  }

  onBack(): void {
    this.wizardService.previousStep();
  }

  onCancel(): void {
    this.wizardService.exitWizard();
  }

  getProtocolLabel(protocol: CommunicationProtocol): string {
    const found = this.protocols.find(p => p.value === protocol);
    return found?.label || 'Unbekannt';
  }
}
