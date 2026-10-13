import { Component, OnInit, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { PageScanService } from '../../services/page-scan.service';
import { DashboardService } from '../../services/dashboard.service';
import { Chart, registerables } from 'chart.js';
import * as L from 'leaflet';

Chart.register(...registerables);

export interface WeeklyStat {
  weekStart: string;
  count: number;
}

export interface LocationStatistic {
  country: string;
  city: string;
  count: number;
}

export interface DashboardStatisticDto {
  totalUser: number;
  totalAudits: number;
  totalAnonymousScans: number;
  avgProcessingTimeMs: number;
  scansThisWeek: number;
  newUsersThisWeek: number;
  scansPerWeek: WeeklyStat[];
  newUsersPerWeek: WeeklyStat[];
  locationData: LocationStatistic[];
}

// Minimal lat/lng lookup for known cities. Add more as your data grows.
const CITY_COORDS: Record<string, { lat: number; lng: number }> = {
  'dhaka|bangladesh': { lat: 23.8103, lng: 90.4125 },
  'chittagong|bangladesh': { lat: 22.3569, lng: 91.7832 },
  'dubai|united arab emirates': { lat: 25.2048, lng: 55.2708 },
  'abu dhabi|united arab emirates': { lat: 24.4539, lng: 54.3773 },
  'riyadh|saudi arabia': { lat: 24.7136, lng: 46.6753 },
  'jeddah|saudi arabia': { lat: 21.4858, lng: 39.1925 },
  'new york|united states': { lat: 40.7128, lng: -74.006 },
  'london|united kingdom': { lat: 51.5074, lng: -0.1278 },
};

// Fix Leaflet's default marker icon paths breaking under Angular's build
const DefaultIcon = L.icon({
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
});
L.Marker.prototype.options.icon = DefaultIcon;

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.css'],
  standalone: false,
})
export class AdminDashboardComponent implements OnInit {
  @ViewChild('trendChart') trendChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('mapContainer') mapContainerRef!: ElementRef<HTMLDivElement>;

  user: any;
  stats: DashboardStatisticDto | null = null;
  loading = true;
  private chart: Chart | null = null;
  private map: L.Map | null = null;

  constructor(
    private router: Router,
    private authService: AuthService,
    private pageScanService: PageScanService,
    private dashboardService: DashboardService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    const userData = sessionStorage.getItem('user');
    if (userData) {
      this.user = JSON.parse(userData);
    }
    this.loadStats();
  }

  loadStats(): void {
    this.loading = true;

    this.dashboardService.getAdminOverview().subscribe({
      next: (res: DashboardStatisticDto) => {
        this.stats = res;
        this.loading = false;

        // Force DOM to update NOW before grabbing ViewChild elements
        this.cdr.detectChanges();

        this.renderChart();
        this.renderMap(res.locationData);
      },
      error: (err) => {
        console.error(err);
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  renderChart(): void {
    if (!this.stats || !this.trendChartRef?.nativeElement) return;

    const labels = this.stats.scansPerWeek.map((w) =>
      new Date(w.weekStart).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
    );
    const scanData = this.stats.scansPerWeek.map((w) => w.count);
    const userData = this.stats.newUsersPerWeek.map((w) => w.count);

    if (this.chart) {
      this.chart.destroy();
    }

    this.chart = new Chart(this.trendChartRef.nativeElement, {
      type: 'line',
      data: {
        labels,
        datasets: [
          {
            label: 'Scans',
            data: scanData,
            borderColor: '#0d4f4a',
            backgroundColor: 'rgba(13,79,74,0.08)',
            tension: 0.35,
            fill: true,
            pointBackgroundColor: '#0d4f4a',
            pointRadius: 4,
          },
          {
            label: 'New Users',
            data: userData,
            borderColor: '#7bb02f',
            backgroundColor: 'rgba(123,176,47,0.08)',
            tension: 0.35,
            fill: true,
            pointBackgroundColor: '#7bb02f',
            pointRadius: 4,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'top',
            labels: { usePointStyle: true, boxWidth: 8, font: { weight: 'bold' } },
          },
        },
        scales: {
          y: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: '#eef2f5' } },
          x: { grid: { display: false } },
        },
      },
    });
  }

  // =========================================================
  // MAP (Leaflet)
  // =========================================================

  renderMap(locations: LocationStatistic[]): void {
    if (!this.mapContainerRef?.nativeElement) return;

    const pins = locations
      .map((loc) => {
        const key = `${loc.city.trim().toLowerCase()}|${loc.country.trim().toLowerCase()}`;
        const coords = CITY_COORDS[key];
        return coords ? { ...loc, ...coords } : null;
      })
      .filter((p): p is LocationStatistic & { lat: number; lng: number } => p !== null);

    // destroy previous instance if reloading
    if (this.map) {
      this.map.remove();
      this.map = null;
    }

    this.map = L.map(this.mapContainerRef.nativeElement, {
      scrollWheelZoom: false,
    }).setView([20, 30], 2);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
      maxZoom: 18,
    }).addTo(this.map);

    if (pins.length === 0) return;

    const markers: L.Marker[] = [];

    pins.forEach((pin) => {
      const marker = L.marker([pin.lat, pin.lng])
        .addTo(this.map!)
        .bindPopup(`<strong>${pin.city}, ${pin.country}</strong><br>${pin.count} scan(s)`);
      markers.push(marker);
    });

    // fit map to show all pins nicely
    const group = L.featureGroup(markers);
    this.map.fitBounds(group.getBounds().pad(0.3));
  }

  // =========================================================
  // FORMATTING
  // =========================================================

  formatDuration(ms: number): string {
    if (!ms || ms <= 0) return '0:00';
    const totalSeconds = Math.round(ms / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  }

  get topLocations(): LocationStatistic[] {
    return this.stats?.locationData?.slice(0, 8) ?? [];
  }

  get maxLocationCount(): number {
    if (!this.stats?.locationData?.length) return 1;
    return Math.max(...this.stats.locationData.map((l) => l.count));
  }

  locationBarWidth(count: number): string {
    return `${Math.max((count / this.maxLocationCount) * 100, 6)}%`;
  }

  logout(): void {
    this.authService.logout();
  }
}
