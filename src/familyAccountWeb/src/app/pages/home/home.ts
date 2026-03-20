import {
  Component,
  inject,
  OnInit,
  AfterViewInit,
  OnDestroy,
  ViewChild,
  ElementRef,
  Injectable,
  ChangeDetectorRef,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FormsModule } from "@angular/forms";
import { NgSelectModule } from "@ng-select/ng-select";
import { addIcons } from "ionicons";
import {
  AppSettings,
  LoggerService,
  ElasticsearchService,
  ElasticsearchWebSocketService,
  SendGridService,
  CampaignService,
  CampaignProgressService,
} from "../../service";
import {
  HeaderComponent,
  FooterComponent,
  PanelComponent,
} from "../../components";
import { InvoicesComponent, AddressesComponent, GeneralStatsComponent, CampaignFormModalComponent, EmailPreviewModalComponent, ClientFilterComponent, ClientListComponent } from "./components";
import { CampaignFormData } from "./components/campaign-form-modal/campaign-form-modal.component";
import { CampaignRequest } from "../../service/campaign-progress.service";
import { ExportProgressService } from "../../service/export-progress.service";
import {
  ColumnMode,
  DatatableComponent,
  NgxDatatableModule,
} from "@swimlane/ngx-datatable";
import {
  NgbDateAdapter,
  NgbDateStruct,
  NgbDatepickerModule,
} from "@ng-bootstrap/ng-bootstrap";
import { NgxDaterangepickerMd, LocaleService, LOCALE_CONFIG, DefaultLocaleConfig } from 'ngx-daterangepicker-material';
import moment from 'moment';
import {
  ClienteItem,
  PaginationInfo,
  LocationItem,
  RestaurantItem,
  DeviceItem,
  ProductFilterItem,
  DeliveryTypeItem,
  TimeOfDayItem,
  ChatMessage,
  DaysRange,
  ClientsFilter,
  InvoiceItem,
  AddressItem,
  GeneralStatsItem,
  SenderInfo,
  SuppressionGroupInfo,
} from "../../shared/models";
import {
  filterOutline,
  peopleOutline,
  personOutline,
  chevronDownCircleOutline,
  refreshOutline,
  statsChartOutline,
  closeCircleOutline,
  callOutline,
  mailOutline,
  cartOutline,
  timeOutline,
  swapVerticalOutline,
  arrowUpOutline,
  arrowDownOutline,
  eyeOutline,
  locationOutline,
  arrowBackOutline,
  receiptOutline,
  calendarOutline,
  restaurantOutline,
  carOutline,
  sparkles,
  search,
  flashOutline,
  checkmarkCircleOutline,
  alertCircleOutline,
  bulbOutline,
  chevronUpOutline,
  swapHorizontalOutline,
  chatbubblesOutline,
  constructOutline,
  trashOutline,
  sendOutline,
  hourglassOutline,
} from "ionicons/icons";
import {
  IonContent,
  IonCard,
  IonCardContent,
  IonItem,
  IonLabel,
  IonIcon,
  IonBadge,
  IonSpinner,
  IonRefresher,
  IonRefresherContent,
  IonInput,
  IonButton,
  IonSegment,
  IonSegmentButton,
} from "@ionic/angular/standalone";
import { ResponsiveComponent } from "../../shared";
import { finalize } from "rxjs";

// Adapter para ng-bootstrap datepicker
@Injectable()
export class NgbDateNativeAdapter extends NgbDateAdapter<Date> {
  fromModel(date: Date | null): NgbDateStruct | null {
    return date && date.getFullYear
      ? {
          year: date.getFullYear(),
          month: date.getMonth() + 1,
          day: date.getDate(),
        }
      : null;
  }

  toModel(date: NgbDateStruct | null): Date | null {
    return date ? new Date(date.year, date.month - 1, date.day) : null;
  }
}

@Component({
  selector: "home",
  templateUrl: "./home.html",
  styleUrls: ["./home.scss"],
  standalone: true,
  providers: [
    { provide: NgbDateAdapter, useClass: NgbDateNativeAdapter },
    { provide: LOCALE_CONFIG, useValue: DefaultLocaleConfig },
    LocaleService,
  ],
  imports: [
    CommonModule,
    FormsModule,
    NgbDatepickerModule,
    NgSelectModule,
    NgxDaterangepickerMd,
    HeaderComponent,
    FooterComponent,
    PanelComponent,
    InvoicesComponent,
    AddressesComponent,
    GeneralStatsComponent,
    CampaignFormModalComponent,
    EmailPreviewModalComponent,
    ClientFilterComponent,
    ClientListComponent,
    NgxDatatableModule,
    IonContent,
    IonCard,
    IonCardContent,
    IonItem,
    IonLabel,
    IonIcon,
    IonBadge,
    IonSpinner,
    IonRefresher,
    IonRefresherContent,
    IonInput,
    IonButton,
    IonSegment,
    IonSegmentButton,
    TranslatePipe,
  ]
})
export class HomePage
  extends ResponsiveComponent
  implements OnInit, AfterViewInit, OnDestroy
{
  // Referencia al componente de lista (para resetear offset de la tabla)
  @ViewChild(ClientListComponent) clientList!: ClientListComponent;

  // Servicios
  private readonly logger = inject(LoggerService).getLogger("HomePage");
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly elasticsearchService = inject(ElasticsearchService);
  private readonly elasticsearchWsService = inject(ElasticsearchWebSocketService);
  private readonly sendgridService = inject(SendGridService);
  private readonly campaignService = inject(CampaignService);
  private readonly campaignProgressService = inject(CampaignProgressService);
  private readonly exportProgressService = inject(ExportProgressService);
  private readonly translate = inject(TranslateService);

  // Estado de navegación
  currentView: "clients" | "clientsFilter" | "invoices" | "addresses" = "clients";
  mobileTab: "chat" | "filters" = "chat"; // Para tabs móvil
  selectedClient: ClienteItem | null = null;

  // Estado de datos
  clients: ClienteItem[] = [];
  totalClients: number = 0;
  totalInvoices: number = 0;
  pagination: PaginationInfo | null = null;
  provinces: LocationItem[] = [];
  cantons: LocationItem[] = [];
  districts: LocationItem[] = [];
  neighborhoods: LocationItem[] = [];
  restaurants: RestaurantItem[] = [];
  devices: DeviceItem[] = [];
  products: ProductFilterItem[] = [];

  // Recursos de SendGrid
  senders: SenderInfo[] = [];
  suppressionGroups: SuppressionGroupInfo[] = [];
  loadingSenders: boolean = false;
  loadingGroups: boolean = false;

  // Estadísticas generales
  generalStats: GeneralStatsItem | null = null;
  loadingGeneralStats: boolean = false;

  // Datos de facturas y direcciones
  invoices: InvoiceItem[] = [];
  addresses: AddressItem[] = [];
  loadingInvoices: boolean = false;
  loadingAddresses: boolean = false;

  // Infinite scroll para móvil - facturas
  hasMoreInvoices: boolean = true;
  currentInvoicesPage: number = 1;
  invoicesPageSize: number = 100;
  totalInvoicesFromServer: number = 0; // Total del servidor para facturas

  // Infinite scroll para móvil - direcciones
  hasMoreAddresses: boolean = true;
  currentAddressesPage: number = 1;
  addressesPageSize: number = 10;
  totalAddressesFromServer: number = 0; // Total del servidor para direcciones

  // Estado de UI
  loading: boolean = false;
  loadingProvinces: boolean = false;
  loadingCantons: boolean = false;
  loadingDistricts: boolean = false;
  loadingNeighborhoods: boolean = false;
  loadingRestaurants: boolean = false;
  loadingDevices: boolean = false;
  loadingProducts: boolean = false;
  loadingTimesOfDay: boolean = false;
  loadingDeliveryTypes: boolean = false;

  // Exportación de datos
  isExportingInvalidContacts: boolean = false;

  // Creación de campañas
  showCampaignForm: boolean = false;

  // Vista previa de email
  showPreviewModal: boolean = false;
  loadingPreview: boolean = false;
  previewHtml: string = '';
  previewError: string = '';

  // Filtros
  selectedFilter: string = "all"; // 'all', 'registered', 'guest'
  clientTypeOptions = [
    { value: 'all', label: 'Todos los clientes' },
    { value: 'registered', label: 'Usuarios registrados' },
    { value: 'guest', label: 'Usuarios invitados' },
  ];
  selectedPaid: boolean | null = null;
  selectedDeliveryTypes: string[] = [];
  selectedProvince: string | null = null;
  selectedCanton: string | null = null;
  selectedDistrict: string | null = null;
  selectedNeighborhood: string | null = null;
  selectedTimeOfDay: string[] = [];
  selectedRestaurant: string[] = [];
  selectedDevice: string[] = [];
  selectedProducts: string[] = [];
  selectedExcludedProducts: string[] = [];
  selectedCustomers: string[] = [];
  daysRange: DaysRange = { min: null, max: null };
  ordersRange: { min: number | null; max: number | null } = {
    min: null,
    max: null,
  };
  dateRange: { from: string | null; to: string | null } = {
    from: null,
    to: null,
  };

  // NgbDatepicker para versión desktop
  dateRangeFrom: Date | null = null;
  dateRangeTo: Date | null = null;

  // ngx-daterangepicker-material: rango de fechas en un único input
  selectedDateRange: { startDate: any; endDate: any } | null = null;
  dateRangeLocale = {
    applyLabel: 'Aplicar',
    clearLabel: 'Limpiar',
    format: 'DD/MM/YYYY',
    separator: ' - ',
    cancelLabel: 'Cancelar',
    daysOfWeek: ['Do', 'Lu', 'Ma', 'Mi', 'Ju', 'Vi', 'Sá'],
    monthNames: ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
      'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'],
    firstDay: 1,
  };

  // Ordenamiento
  sortField: string = "nameCustomer";
  sortOrder: "asc" | "desc" = "asc";
  sorts: any[] = [{ prop: "nameCustomer", dir: "asc" }];

  // Opciones de ordenamiento para móvil
  sortOptions = [
    { value: "idCustomer", label: "ID Cliente" },
    { value: "nameCustomer", label: "Nombre" },
    { value: "phoneCustomer", label: "Teléfono" },
    { value: "emailCustomer", label: "Correo" },
    { value: "totalPurchases", label: "Cantidad Compras" },
    { value: "totalSpent", label: "Monto Compras" },
    { value: "daysSinceLastPurchase", label: "Días sin Comprar" },
  ];

  // Para infinite scroll (móvil) y paginación
  hasMore: boolean = true;
  private currentPage: number = 1;
  clientsPageSize: number = 10;

  // Búsqueda y autocomplete
  searchQuery: string = "";
  isSearchMode: boolean = false;
  isSearching: boolean = false;
  showAutocomplete: boolean = false;
  autocompleteResults: any[] = [];
  private searchTimeout: any = null;
  private autocompleteTimeout: any = null;

  // Chat MCP con IA
  mcpCurrentMessage: string = "";
  
  // Estado WebSocket
  useWebSocket: boolean = true;

  // Estado WebSocket Chat (ahora manejado por el servicio)
  @ViewChild('chatContainer') chatContainer?: ElementRef;

  // Opciones cargadas desde Elasticsearch
  deliveryTypes: DeliveryTypeItem[] = [];
  timesOfDay: TimeOfDayItem[] = [];

  // Para ngx-datatable
  ColumnMode = ColumnMode;
  columns = [
    { prop: "idCustomer", name: "ID", width: 80, sortable: true },
    { prop: "nameCustomer", name: "Nombre", sortable: true },
    { prop: "phoneCustomer", name: "Teléfono", width: 120, sortable: true },
    { prop: "emailCustomer", name: "Correo", sortable: true },
    { prop: "totalPurchases", name: "Compras", width: 100, sortable: true },
    { prop: "totalSpent", name: "Monto Total", width: 120, sortable: true },
    {
      prop: "daysSinceLastPurchase",
      name: "Días sin Comprar",
      width: 140,
      sortable: true,
    },
  ];

  constructor(public appSettings: AppSettings) {
    super();
    // Registrar íconos de Ionic
    addIcons({
      statsChartOutline,
      chatbubblesOutline,
      constructOutline,
      trashOutline,
      sendOutline,
      hourglassOutline,
      filterOutline,
      refreshOutline,
      swapVerticalOutline,
      peopleOutline,
      personOutline,
      callOutline,
      mailOutline,
      cartOutline,
      timeOutline,
      receiptOutline,
      locationOutline,
      sparkles,
      search,
      flashOutline,
      closeCircleOutline,
      checkmarkCircleOutline,
      alertCircleOutline,
      bulbOutline,
      swapHorizontalOutline,
      calendarOutline,
      restaurantOutline,
      carOutline,
      chevronDownCircleOutline,
      chevronUpOutline,
      arrowUpOutline,
      arrowDownOutline,
      eyeOutline,
      arrowBackOutline,
    });
  }

  // ==================== FUNCIONES TRACKBY PARA RENDIMIENTO ====================
  
  /**
   * TrackBy function para lista de clientes
   */
  trackByClientId(index: number, item: ClienteItem): string | number {
    // Priorizar idCustomer si está disponible
    if (item?.idCustomer) {
      return `client_${item.idCustomer}`;
    }
    
    // Si hay teléfono, combinarlo con índice para evitar duplicados
    if (item?.phoneCustomer && item.phoneCustomer.trim() !== '') {
      return `phone_${item.phoneCustomer.trim()}_${index}`;
    }
    
    // Si hay email, combinarlo con índice
    if (item?.emailCustomer && item.emailCustomer.trim() !== '') {
      return `email_${item.emailCustomer.trim()}_${index}`;
    }
    
    // Si hay nombre, combinarlo con índice
    if (item?.nameCustomer && item.nameCustomer.trim() !== '') {
      return `name_${item.nameCustomer.trim()}_${index}`;
    }
    
    // Fallback: usar siempre el índice para garantizar unicidad
    return `fallback_${index}`;
  }



  /**
   * TrackBy function para lista de restaurantes
   */
  trackByRestaurantId(index: number, item: RestaurantItem): string | number {
    return item?.name || `restaurant_${index}`;
  }

  /**
   * TrackBy function para lista de dispositivos
   */
  trackByDeviceId(index: number, item: DeviceItem): string | number {
    return item?.name || `device_${index}`;
  }

  /**
   * TrackBy function para lista de productos
   */
  trackByProductId(index: number, item: ProductFilterItem): string | number {
    return item?.name || `product_${index}`;
  }

  /**
   * TrackBy function para la tabla de clientes (ngx-datatable)
   */
  trackByClientTable = (index: number, item: ClienteItem): string | number => {
    // Priorizar idCustomer si está disponible
    if (item?.idCustomer) {
      const key = `table_client_${item.idCustomer}`;
      return key;
    }
    
    // Si hay teléfono, combinarlo con índice para evitar duplicados
    if (item?.phoneCustomer && item.phoneCustomer.trim() !== '') {
      const key = `table_phone_${item.phoneCustomer.trim()}_${index}`;
      return key;
    }
    
    // Si hay email, combinarlo con índice
    if (item?.emailCustomer && item.emailCustomer.trim() !== '') {
      const key = `table_email_${item.emailCustomer.trim()}_${index}`;
      return key;
    }
    
    // Si hay nombre, combinarlo con índice
    if (item?.nameCustomer && item.nameCustomer.trim() !== '') {
      const key = `table_name_${item.nameCustomer.trim()}_${index}`;
      return key;
    }
    
    // Fallback: usar siempre el índice para garantizar unicidad
    const key = `table_fallback_${index}`;
    return key;
  }

  /**
   * TrackBy function para mensajes de chat MCP
   */
  trackByChatMessageId(index: number, item: ChatMessage): string | number {
    // Siempre incluir el índice para evitar duplicados
    if (item?.id) {
      return item.id;
    }
    const timestamp = item?.timestamp;
    const timestampStr = timestamp instanceof Date ? timestamp.getTime().toString() : String(timestamp || 'msg');
    return `${timestampStr}_${index}`;
  }

  // Getter para estado WebSocket
  get wsConnected(): boolean {
    // Usar una propiedad local o el valor más reciente del observable
    let connected = false;
    this.elasticsearchWsService.connected$.subscribe(val => connected = val).unsubscribe();
    return connected;
  }

  // Getter para estado de carga del chat MCP
  get mcpChatLoading(): boolean {
    let loading = false;
    this.elasticsearchWsService.loading$.subscribe(val => loading = val).unsubscribe();
    return loading;
  }

  // Función para alternar modo WebSocket
  toggleWebSocketMode(): void {
    this.useWebSocket = !this.useWebSocket;
    if (this.useWebSocket) {
      this.connectWebSocket();
    } else {
      this.disconnectWebSocket();
    }
  }

  // Función helper para toggle de conexión WebSocket (para el botón)
  toggleWebSocketConnection(): void {
    if (this.wsConnected) {
      this.disconnectWebSocket();
    } else {
      this.connectWebSocket();
    }
  }

  /**
   * Deduplicar array de clientes basado en idCustomer y teléfono
   */
  private deduplicateClients(clients: ClienteItem[]): ClienteItem[] {
    const seen = new Set<string>();
    const uniqueClients: ClienteItem[] = [];
    
    for (const client of clients) {
      // Crear clave única para identificar duplicados
      let uniqueKey = '';
      
      if (client?.idCustomer) {
        uniqueKey = `id_${client.idCustomer}`;
      } else if (client?.phoneCustomer && client.phoneCustomer.trim() !== '') {
        uniqueKey = `phone_${client.phoneCustomer.trim()}`;
      } else if (client?.emailCustomer && client.emailCustomer.trim() !== '') {
        uniqueKey = `email_${client.emailCustomer.trim()}`;
      } else {
        // Para elementos sin identificadores únicos, generar una basada en múltiples campos
        const name = client?.nameCustomer?.trim() || '';
        const phone = client?.phoneCustomer?.trim() || '';
        const email = client?.emailCustomer?.trim() || '';
        uniqueKey = `combo_${name}_${phone}_${email}`;
      }
      
      if (!seen.has(uniqueKey)) {
        seen.add(uniqueKey);
        uniqueClients.push(client);
      } else {
        this.logger.warn('Cliente duplicado eliminado:', client);
      }
    }
    
    return uniqueClients;
  }

  /**
   * Manejar actualización de tabla desde WebSocket
   * Reutiliza handleClientResponse ya que la estructura es idéntica al API REST
   * Además actualiza las variables de filtros para sincronizar la UI
   */
  handleClientsTableUpdate(event: any): void {
    try {
      this.logger.debug('Recibido evento de actualización de tabla:', event);
      
      // Validar estructura del evento
      if (!event || !event.data) {
        this.logger.warn('Evento de actualización inválido: estructura malformada');
        return;
      }
      
      // Extraer datos del evento - misma estructura que API REST
      const eventData = event.data;
      
      // Actualizar filtros si vienen en appliedFilters
      if (eventData.appliedFilters) {
        this.updateFiltersFromApplied(eventData.appliedFilters);
      }
      
      // Reutilizar handleClientResponse ya que la estructura es idéntica
      // reset=true porque siempre reemplazamos los datos desde WebSocket
      this.handleClientResponse(eventData, true);
      
      this.logger.success('Tabla actualizada desde WebSocket via handleClientResponse');
      
    } catch (error) {
      this.logger.error('Error procesando actualización de tabla:', error);
    }
  }

  /**
   * Actualizar variables de filtros desde appliedFilters
   * Permite sincronizar la UI cuando el WebSocket envía filtros aplicados
   */
  private updateFiltersFromApplied(appliedFilters: any): void {
    try {
      this.logger.debug('Actualizando filtros desde appliedFilters:', appliedFilters);

      // Filtro de tipo de cliente (registered)
      if (appliedFilters.registered !== undefined && appliedFilters.registered !== null) {
        this.selectedFilter = appliedFilters.registered ? 'registered' : 'guest';
      } else {
        this.selectedFilter = 'all';
      }

      // Filtros simples
      this.selectedPaid = appliedFilters.paid ?? null;
      this.selectedProvince = appliedFilters.province ?? null;
      this.selectedCanton = appliedFilters.canton ?? null;
      this.selectedDistrict = appliedFilters.district ?? null;
      this.selectedNeighborhood = appliedFilters.neighborhood ?? null;
      this.selectedTimeOfDay = appliedFilters.timeOfDay ?? [];
      this.selectedRestaurant = appliedFilters.restaurant ?? [];
      this.selectedDevice = appliedFilters.device ?? [];

      // Filtros de array
      this.selectedDeliveryTypes = appliedFilters.deliveryTypes ?? [];
      this.selectedProducts = appliedFilters.products ?? [];
      this.selectedExcludedProducts = appliedFilters.excludedProducts ?? [];
      this.selectedCustomers = appliedFilters.customers ?? [];

      // Rangos numéricos
      this.daysRange = {
        min: appliedFilters.daysSinceMin ?? null,
        max: appliedFilters.daysSinceMax ?? null
      };

      this.ordersRange = {
        min: appliedFilters.ordersMin ?? null,
        max: appliedFilters.ordersMax ?? null
      };

      // Rangos de fechas (para versión móvil)
      this.dateRange = {
        from: appliedFilters.dateFrom ?? null,
        to: appliedFilters.dateTo ?? null
      };

      // Paginación
      if (appliedFilters.pageSize && appliedFilters.pageSize > 0) {
        this.clientsPageSize = appliedFilters.pageSize;
        this.logger.debug('Tamaño de página actualizado:', this.clientsPageSize);
      }

      // Rangos de fechas (para versión desktop - convertir strings a Date)
      // Usar local midnight para evitar desfase de timezone con new Date("YYYY-MM-DD") que es UTC
      if (appliedFilters.dateFrom) {
        try {
          const [y, m, d] = (appliedFilters.dateFrom as string).split('-').map(Number);
          this.dateRangeFrom = new Date(y, m - 1, d);
        } catch (e) {
          this.dateRangeFrom = null;
        }
      } else {
        this.dateRangeFrom = null;
      }

      if (appliedFilters.dateTo) {
        try {
          const [y, m, d] = (appliedFilters.dateTo as string).split('-').map(Number);
          this.dateRangeTo = new Date(y, m - 1, d);
        } catch (e) {
          this.dateRangeTo = null;
        }
      } else {
        this.dateRangeTo = null;
      }

      // Sincronizar selectedDateRange para el picker de rango.
      // Usar moment(string) — ngx-daterangepicker-material v6 requiere objetos moment, no dayjs.
      if (appliedFilters.dateFrom || appliedFilters.dateTo) {
        this.selectedDateRange = {
          startDate: appliedFilters.dateFrom ? moment(appliedFilters.dateFrom as string) : null,
          endDate: appliedFilters.dateTo ? moment(appliedFilters.dateTo as string) : null,
        };
      } else {
        this.selectedDateRange = null;
      }

      // Actualizar listas dependientes si cambiaron las ubicaciones
      if (appliedFilters.province !== this.selectedProvince) {
        if (appliedFilters.province) {
          this.loadCantons(appliedFilters.province);
        } else {
          this.cantons = [];
          this.districts = [];
          this.neighborhoods = [];
        }
      }

      if (appliedFilters.canton && appliedFilters.province) {
        if (appliedFilters.canton !== this.selectedCanton) {
          this.loadDistricts(appliedFilters.province, appliedFilters.canton);
        }
      } else {
        this.districts = [];
        this.neighborhoods = [];
      }

      if (appliedFilters.district && appliedFilters.province && appliedFilters.canton) {
        if (appliedFilters.district !== this.selectedDistrict) {
          this.loadNeighborhoods(appliedFilters.province, appliedFilters.canton, appliedFilters.district);
        }
      } else {
        this.neighborhoods = [];
      }

      this.logger.success('Filtros actualizados correctamente');
    } catch (error) {
      this.logger.error('Error actualizando filtros:', error);
    }
  }

  ngOnInit(): void {
    // Inicializar servicio de chat WebSocket
    this.elasticsearchWsService.initialize();
    
    // Suscribirse a cambios en los mensajes para hacer scroll automático
    this.elasticsearchWsService.messages$.subscribe(() => {
      this.scrollToBottom();
    });
    
    // Suscribirse a eventos personalizados del WebSocket
    this.elasticsearchWsService.customEvents$.subscribe(event => {
      if (event?.type === 'clients_table_update') {
        this.handleClientsTableUpdate(event);
      }
      else if (event?.type === 'clients_data') {
        // Datos estructurados del chat MCP (elasticsearch_list_clients)
        this.logger.success('📊 Actualizando tabla desde chat MCP', event.data);
        this.handleClientsTableUpdate(event);
      }
    });
    
    this.loadGeneralStats();
    this.loadProvinces();
    this.loadRestaurants();
    this.loadDevices();
    this.loadProducts();
    this.loadTimesOfDay();
    this.loadDeliveryTypes();
    this.loadSendGridResources();
    this.loadClients();

    // Listener para cerrar autocomplete al hacer clic fuera
    document.addEventListener("click", this.handleDocumentClick.bind(this));
  }

  ngAfterViewInit(): void {
    // Ya no necesitamos inicializar select2
  }

  private handleDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    const searchInput = document.querySelector(
      'input[placeholder*="Buscar clientes"]',
    );
    const autocompleteDropdown = document.querySelector(".dropdown-menu.show");

    // Si el clic no fue en el input de búsqueda ni en el dropdown, cerrar autocomplete
    if (
      searchInput &&
      !searchInput.contains(target) &&
      autocompleteDropdown &&
      !autocompleteDropdown.contains(target)
    ) {
      this.showAutocomplete = false;
    }
  }

  // ==================== FILTROS ====================
  onFilterChange(filterValue: string): void {
    this.selectedFilter = filterValue;
    this.resetPagination();
    this.loadClients(true);
  }



  onPaidChange(paid: boolean | null): void {
    this.selectedPaid = paid;
    this.resetPagination();
    this.loadClients(true);
  }

  onDeliveryFilterChange(deliveryType: string, checked: boolean): void {
    if (checked) {
      if (!this.selectedDeliveryTypes.includes(deliveryType)) {
        this.selectedDeliveryTypes.push(deliveryType);
      }
    } else {
      this.selectedDeliveryTypes = this.selectedDeliveryTypes.filter(
        (type) => type !== deliveryType,
      );
    }
    this.resetPagination();
    this.loadClients(true);
  }

  onProvinceChange(province: string | null): void {
    this.selectedProvince = province;
    // Limpiar filtros dependientes
    this.selectedCanton = null;
    this.selectedDistrict = null;
    this.selectedNeighborhood = null;
    this.cantons = [];
    this.districts = [];
    this.neighborhoods = [];

    if (province) {
      this.loadCantons(province);
    }
    this.resetPagination();
    this.loadClients(true);
  }

  onCantonChange(canton: string | null): void {
    this.selectedCanton = canton;
    // Limpiar filtros dependientes
    this.selectedDistrict = null;
    this.selectedNeighborhood = null;
    this.districts = [];
    this.neighborhoods = [];

    if (canton && this.selectedProvince) {
      this.loadDistricts(this.selectedProvince, canton);
    }
    this.resetPagination();
    this.loadClients(true);
  }

  onDistrictChange(district: string | null): void {
    this.selectedDistrict = district;
    // Limpiar filtros dependientes
    this.selectedNeighborhood = null;
    this.neighborhoods = [];

    if (district && this.selectedProvince && this.selectedCanton) {
      this.loadNeighborhoods(
        this.selectedProvince,
        this.selectedCanton,
        district,
      );
    }
    this.resetPagination();
    this.loadClients(true);
  }

  onNeighborhoodChange(neighborhood: string | null): void {
    this.selectedNeighborhood = neighborhood;
    this.resetPagination();
    this.loadClients(true);
  }

  onOtherFilterChange(filter: string, value: any): void {
    switch (filter) {
      case "timeOfDay":
        this.selectedTimeOfDay = value;
        break;
      case "restaurant":
        this.selectedRestaurant = value;
        break;
      case "device":
        this.selectedDevice = value;
        break;
      case "deliveryType":
        this.selectedDeliveryTypes = value;
        break;
    }
    this.resetPagination();
    this.loadClients(true);
  }

  onProductsChange(products: string[]): void {
    this.selectedProducts = products;
    this.resetPagination();
    this.loadClients(true);
  }

  onExcludedProductsChange(products: string[]): void {
    this.selectedExcludedProducts = products;
    this.resetPagination();
    this.loadClients(true);
  }

  onCustomersChange(phones: string[]): void {
    this.selectedCustomers = phones;
    this.resetPagination();
    this.loadClients(true);
  }

  // ==================== BÚSQUEDA Y AUTOCOMPLETE ====================
  onSearchQueryChange(query: string): void {
    this.searchQuery = query;

    if (this.autocompleteTimeout) clearTimeout(this.autocompleteTimeout);
    if (this.searchTimeout) clearTimeout(this.searchTimeout);

    this.isSearchMode = query.trim().length > 0;

    if (!this.isSearchMode) {
      this.showAutocomplete = false;
      this.autocompleteResults = [];
      // Al limpiar búsqueda, refrescar resultados
      this.isSearching = false;
      this.resetPagination();
      this.loadClients(true);
      return;
    }

    // Buscar inmediatamente (solo se llama desde Enter)
    this.isSearching = true;
    this.resetPagination();
    this.loadClients(true);
  }

  async loadAutocomplete(query: string): Promise<void> {
    if (!query || query.length < 2) return;

    try {
      const response = await this.elasticsearchService.autocompleteClients(query, 10);
      if (response.success) {
        this.autocompleteResults = response.suggestions;
      }
    } catch (error) {
      this.logger.error("Error en autocomplete:", error);
      this.autocompleteResults = [];
    }
  }

  selectAutocompleteResult(result: any): void {
    this.showAutocomplete = false;
    this.autocompleteResults = [];
    this.searchQuery = result.name;
    this.isSearchMode = true;
    this.resetPagination();
    this.loadClients(true);
  }

  clearSearch(): void {
    this.searchQuery = "";
    this.isSearchMode = false;
    this.isSearching = false;
    this.showAutocomplete = false;
    this.autocompleteResults = [];

    if (this.autocompleteTimeout) clearTimeout(this.autocompleteTimeout);
    if (this.searchTimeout) clearTimeout(this.searchTimeout);

    this.resetPagination();
    this.loadClients(true);
  }

  // ==================== CHAT MCP CON IA ====================

  onDaysRangeChange(): void {
    this.resetPagination();
    this.loadClients(true);
  }

  onOrdersRangeChange(): void {
    this.resetPagination();
    this.loadClients(true);
  }

  onDateRangeChange(): void {
    // Convertir Date a string formato YYYY-MM-DD para el filtro
    if (this.dateRangeFrom) {
      this.dateRange.from = this.formatDateToString(this.dateRangeFrom);
    } else {
      this.dateRange.from = null;
    }

    if (this.dateRangeTo) {
      this.dateRange.to = this.formatDateToString(this.dateRangeTo);
    } else {
      this.dateRange.to = null;
    }

    this.resetPagination();
    this.loadClients(true);
  }

  onDateRangePickerChange(event: { startDate: any; endDate: any } | null): void {
    this.dateRangeFrom = event?.startDate ? event.startDate.toDate() : null;
    this.dateRangeTo = event?.endDate ? event.endDate.toDate() : null;
    // Sincronizar selectedDateRange para que ngOnChanges no restaure el valor anterior
    this.selectedDateRange = event
      ? { startDate: event.startDate, endDate: event.endDate }
      : null;
    this.onDateRangeChange();
  }

  onDateFromChange(event: any): void {
    // ion-datetime devuelve ISO 8601 (YYYY-MM-DD)
    this.dateRange.from = event.detail.value?.split("T")[0] || null;
    this.onDateRangeChange();
  }

  onDateToChange(event: any): void {
    // ion-datetime devuelve ISO 8601 (YYYY-MM-DD)
    this.dateRange.to = event.detail.value?.split("T")[0] || null;
    this.onDateRangeChange();
  }

  getFilterTitle(): string {
    switch (this.selectedFilter) {
      case "registered":
        return this.translate.instant('HOME.REGISTERED_BADGE');
      case "guest":
        return this.translate.instant('HOME.GUESTS_BADGE');
      default:
        return this.translate.instant('HOME.ALL_FILTER');
    }
  }

  getSelectedCompanyName(): string {
    return this.appSettings.nameCompany || 'N/D';
  }

  private getRegisteredFilter(): boolean | undefined {
    switch (this.selectedFilter) {
      case "registered":
        return true;
      case "guest":
        return false;
      default:
        return undefined;
    }
  }

  // ==================== ORDENAMIENTO ====================
  onSortChange(): void {
    // Para móvil: cambiar entre asc y desc
    this.sortOrder = this.sortOrder === "asc" ? "desc" : "asc";
    this.sorts = [{ prop: this.sortField, dir: this.sortOrder }];
    this.resetPagination();
    this.loadClients(true);
  }

  onSortFieldChange(field: string): void {
    // Para móvil: cambiar campo de ordenamiento
    this.sortField = field;
    this.sorts = [{ prop: this.sortField, dir: this.sortOrder }];
    this.resetPagination();
    this.loadClients(true);
  }

  onSort(event: any): void {
    // Para desktop: evento de ngx-datatable
    if (event.sorts && event.sorts.length > 0) {
      const sort = event.sorts[0];
      this.sortField = sort.prop;
      this.sortOrder = sort.dir;
      this.sorts = event.sorts;
      this.resetPagination();
      this.loadClients(true);
    }
  }

  getSortLabel(): string {
    const option = this.sortOptions.find((opt) => opt.value === this.sortField);
    return option ? option.label : "ID Cliente";
  }

  // ==================== CARGA DE DATOS ====================
  private loadGeneralStats(): void {
    this.loadingGeneralStats = true;
    this.elasticsearchService.getGeneralStats().subscribe({
      next: (response) => {
        if (response.success) {
          this.generalStats = response.stats;
          this.logger.info("Estadísticas generales cargadas", response);
        }
        this.loadingGeneralStats = false;
      },
      error: (error) => {
        this.logger.error("Error al cargar estadísticas generales", error);
        this.loadingGeneralStats = false;
      },
    });
  }



  private loadProvinces(): void {
    this.loadingProvinces = true;

    this.elasticsearchService
      .getProvinces()
      .pipe(finalize(() => (this.loadingProvinces = false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.provinces = response.locations;
            this.logger.debug(`Cargadas ${this.provinces.length} provincias`);
          }
        },
        error: (error) => {
          this.logger.error("Error cargando provincias:", error);
          this.provinces = [];
        },
      });
  }

  private loadCantons(province: string): void {
    this.loadingCantons = true;

    this.elasticsearchService
      .getCantons(province)
      .pipe(finalize(() => (this.loadingCantons = false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.cantons = response.locations;
            this.logger.debug(`Cargados ${this.cantons.length} cantones`);
          }
        },
        error: (error) => {
          this.logger.error("Error cargando cantones:", error);
          this.cantons = [];
        },
      });
  }

  private loadDistricts(province: string, canton: string): void {
    this.loadingDistricts = true;

    this.elasticsearchService
      .getDistricts(province, canton)
      .pipe(finalize(() => (this.loadingDistricts = false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.districts = response.locations;
            this.logger.debug(`Cargados ${this.districts.length} distritos`);
          }
        },
        error: (error) => {
          this.logger.error("Error cargando distritos:", error);
          this.districts = [];
        },
      });
  }

  private loadNeighborhoods(
    province: string,
    canton: string,
    district: string,
  ): void {
    this.loadingNeighborhoods = true;

    this.elasticsearchService
      .getNeighborhoods(
        province,
        canton,
        district,
      )
      .pipe(finalize(() => (this.loadingNeighborhoods = false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.neighborhoods = response.locations;
            this.logger.debug(`Cargados ${this.neighborhoods.length} barrios`);
          }
        },
        error: (error) => {
          this.logger.error("Error cargando barrios:", error);
          this.neighborhoods = [];
        },
      });
  }

  private loadRestaurants(): void {
    this.loadingRestaurants = true;

    this.elasticsearchService
      .getRestaurants()
      .pipe(finalize(() => (this.loadingRestaurants = false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.restaurants = response.restaurants;
            this.logger.debug(
              `Cargados ${this.restaurants.length} restaurantes`,
            );
          }
        },
        error: (error) => {
          this.logger.error("Error cargando restaurantes:", error);
          this.restaurants = [];
        },
      });
  }

  private loadTimesOfDay(): void {
    this.loadingTimesOfDay = true;
    this.elasticsearchService
      .getTimesOfDay()
      .pipe(finalize(() => (this.loadingTimesOfDay = false)))
      .subscribe({
        next: (response) => {
          if (response.success) this.timesOfDay = response.timesOfDay;
        },
        error: (error) => {
          this.logger.error('Error cargando horarios del día:', error);
          this.timesOfDay = [];
        },
      });
  }

  private loadDeliveryTypes(): void {
    this.loadingDeliveryTypes = true;
    this.elasticsearchService
      .getDeliveryTypes()
      .pipe(finalize(() => (this.loadingDeliveryTypes = false)))
      .subscribe({
        next: (response) => {
          if (response.success) this.deliveryTypes = response.deliveryTypes;
        },
        error: (error) => {
          this.logger.error('Error cargando tipos de entrega:', error);
          this.deliveryTypes = [];
        },
      });
  }

  private loadDevices(): void {
    this.loadingDevices = true;

    this.elasticsearchService
      .getDevices()
      .pipe(finalize(() => (this.loadingDevices = false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.devices = response.devices;
            this.logger.debug(`Cargados ${this.devices.length} dispositivos`);
          }
        },
        error: (error) => {
          this.logger.error("Error cargando dispositivos:", error);
          this.devices = [];
        },
      });
  }

  private loadProducts(): void {
    this.loadingProducts = true;

    this.elasticsearchService
      .getProducts(undefined)
      .pipe(finalize(() => (this.loadingProducts = false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.products = response.products;
            this.logger.debug(`Cargados ${this.products.length} productos`);
          }
        },
        error: (error) => {
          this.logger.error("Error cargando productos:", error);
          this.products = [];
        },
      });
  }

  /**
   * Carga recursos de SendGrid (Senders, Designs, Suppression Groups)
   */
  private loadSendGridResources(): void {
    this.logger.info('🔄 Cargando recursos de SendGrid...');
    
    // Cargar Senders
    this.loadingSenders = true;
    this.sendgridService.listSenders()
      .pipe(finalize(() => (this.loadingSenders = false)))
      .subscribe({
        next: (response) => {
          this.senders = response.results || [];
          this.logger.success(`✅ ${this.senders.length} senders cargados`);
        },
        error: (error) => {
          this.logger.error('❌ Error cargando senders:', error);
          this.senders = [];
        }
      });

    // Cargar Suppression Groups
    this.loadingGroups = true;
    this.sendgridService.listSuppressionGroups()
      .pipe(finalize(() => (this.loadingGroups = false)))
      .subscribe({
        next: (response) => {
          this.suppressionGroups = response.groups || [];
          this.logger.success(`✅ ${this.suppressionGroups.length} suppression groups cargados`);
        },
        error: (error) => {
          this.logger.error('❌ Error cargando suppression groups:', error);
          this.suppressionGroups = [];
        }
      });
  }

  private loadClients(reset: boolean = false): void {
    // En móvil, resetear datos para infinite scroll
    // En desktop, mantener datos mientras se cargan los nuevos (el panel muestra el loading)
    if (reset && this.isMobile()) {
      this.resetData();
    }

    this.loading = true;
    this.cdr.detectChanges();

    const filter: ClientsFilter = {
      registered: this.getRegisteredFilter(),
      paid: this.selectedPaid || undefined,
      deliveryTypes:
        this.selectedDeliveryTypes.length > 0
          ? this.selectedDeliveryTypes
          : undefined,
      province: this.selectedProvince || undefined,
      canton: this.selectedCanton || undefined,
      district: this.selectedDistrict || undefined,
      neighborhood: this.selectedNeighborhood || undefined,
      daysSinceMin: this.daysRange.min || undefined,
      daysSinceMax: this.daysRange.max || undefined,
      ordersMin: this.ordersRange.min || undefined,
      ordersMax: this.ordersRange.max || undefined,
      dateFrom: this.dateRange.from
        ? this.formatDateToString(this.dateRange.from)
        : undefined,
      dateTo: this.dateRange.to
        ? this.formatDateToString(this.dateRange.to)
        : undefined,
      timeOfDay: this.selectedTimeOfDay.length > 0 ? this.selectedTimeOfDay : undefined,
      restaurant: this.selectedRestaurant.length > 0 ? this.selectedRestaurant : undefined,
      device: this.selectedDevice.length > 0 ? this.selectedDevice : undefined,
      products:
        this.selectedProducts.length > 0 ? this.selectedProducts : undefined,
      excludedProducts:
        this.selectedExcludedProducts.length > 0 ? this.selectedExcludedProducts : undefined,
      customers: this.selectedCustomers.length > 0 ? this.selectedCustomers : undefined,
      q: this.searchQuery && this.searchQuery.trim().length > 0 ? this.searchQuery.trim() : undefined,
      page: this.currentPage,
      pageSize: this.clientsPageSize,
      sortField: this.sortField,
      sortOrder: this.sortOrder,
    };

    this.elasticsearchService
      .getClients(filter)
      .pipe(finalize(() => { this.loading = false; this.isSearching = false; }))
      .subscribe({
        next: (response) => {
          this.handleClientResponse(response, reset);
        },
        error: (error) => {
          this.logger.error("Error cargando clientes:", error);
          this.handleError();
        },
      });
  }

  private handleClientResponse(response: any, reset: boolean): void {
    if (response.success && response.clients) {
      // Filtrar datos válidos primero
      const validClients = response.clients
        .filter((client: any) => client && (client.idCustomer || client.phoneCustomer || client.emailCustomer || client.nameCustomer))
        .map((client: any) => ({
          ...client,
          // Limpiar campos para consistencia
          nameCustomer: client.nameCustomer?.trim() || '',
          phoneCustomer: client.phoneCustomer?.trim() || '',
          emailCustomer: client.emailCustomer?.trim() || ''
        }));

      // Aplicar deduplicación
      const cleanedClients = this.deduplicateClients(validClients);

      if (reset) {
        // En desktop, reemplazar directamente sin vaciar primero
        // En móvil, ya se vació en loadClients() con resetData()
        this.clients = cleanedClients;
      } else {
        // Para infinite scroll en móvil, agregar al final evitando duplicados
        const combinedClients = [...this.clients, ...cleanedClients];
        this.clients = this.deduplicateClients(combinedClients);
      }

      this.totalClients = response.pagination.totalRecords;
      // El totalInvoices no viene en la respuesta de clientes, mantener el valor actual
      this.pagination = response.pagination;

      // Para infinite scroll
      this.hasMore = response.pagination.hasNext;

      this.logger.debug(
        `Cargados ${cleanedClients.length} clientes únicos de ${validClients.length} válidos. Total: ${this.totalClients}, Facturas: ${this.totalInvoices}`,
      );
    } else {
      this.handleError();
    }
  }

  private handleError(): void {
    if (this.clients.length === 0) {
      this.clients = [];
    }
    // Mostrar toast o alert aquí si es necesario
  }

  // ==================== MÓVIL: REFRESHER & INFINITE SCROLL ====================
  handleRefresh(event: any): void {
    this.logger.debug("Pull to refresh activado");
    this.resetPagination();

    const filter: ClientsFilter = {
      registered: this.getRegisteredFilter(),
      paid: this.selectedPaid || undefined,
      deliveryTypes:
        this.selectedDeliveryTypes.length > 0
          ? this.selectedDeliveryTypes
          : undefined,
      province: this.selectedProvince || undefined,
      canton: this.selectedCanton || undefined,
      district: this.selectedDistrict || undefined,
      neighborhood: this.selectedNeighborhood || undefined,
      daysSinceMin: this.daysRange.min || undefined,
      daysSinceMax: this.daysRange.max || undefined,
      timeOfDay: this.selectedTimeOfDay.length > 0 ? this.selectedTimeOfDay : undefined,
      restaurant: this.selectedRestaurant.length > 0 ? this.selectedRestaurant : undefined,
      device: this.selectedDevice.length > 0 ? this.selectedDevice : undefined,
      page: 1,
      pageSize: this.clientsPageSize,
    };

    this.elasticsearchService
      .getClients(filter)
      .pipe(finalize(() => event.target.complete()))
      .subscribe({
        next: (response) => {
          this.handleClientResponse(response, true);
        },
        error: (error) => {
          this.logger.error("Error en refresh:", error);
          this.handleError();
        },
      });
  }

  loadMore(event: any): void {
    if (!this.hasMore || this.loading) {
      event.target.complete();
      return;
    }

    this.currentPage++;

    const filter: ClientsFilter = {
      registered: this.getRegisteredFilter(),
      paid: this.selectedPaid || undefined,
      deliveryTypes:
        this.selectedDeliveryTypes.length > 0
          ? this.selectedDeliveryTypes
          : undefined,
      province: this.selectedProvince || undefined,
      canton: this.selectedCanton || undefined,
      district: this.selectedDistrict || undefined,
      neighborhood: this.selectedNeighborhood || undefined,
      daysSinceMin: this.daysRange.min || undefined,
      daysSinceMax: this.daysRange.max || undefined,
      timeOfDay: this.selectedTimeOfDay.length > 0 ? this.selectedTimeOfDay : undefined,
      restaurant: this.selectedRestaurant.length > 0 ? this.selectedRestaurant : undefined,
      device: this.selectedDevice.length > 0 ? this.selectedDevice : undefined,
      page: this.currentPage,
      pageSize: this.clientsPageSize,
    };

    this.elasticsearchService
      .getClients(filter)
      .pipe(finalize(() => event.target.complete()))
      .subscribe({
        next: (response) => {
          this.handleClientResponse(response, false);
        },
        error: (error) => {
          this.logger.error("Error en loadMore:", error);
          this.currentPage--; // Revertir página si hay error
        },
      });
  }

  // ==================== DESKTOP: PAGINACIÓN ====================
  /**
   * Manejar evento de paginación de ngx-datatable
   * IMPORTANTE: ngx-datatable usa offset 0-based
   */
  setPage(pageInfo: any): void {
    this.logger.debug("Evento page de ngx-datatable:", pageInfo);

    // Convertir offset 0-based a página 1-based
    const newPage = pageInfo.offset + 1;

    if (newPage === this.currentPage) {
      return; // Ya estamos en esta página
    }

    this.currentPage = newPage;
    this.loadClients(true);
  }

  /**
   * Ir a página específica desde paginación personalizada
   */
  goToPage(page: number): void {
    if (!this.pagination || page < 1 || page > this.pagination.totalPages) {
      return;
    }

    this.currentPage = page;
    this.loadClients(true);
  }

  refreshData(): void {
    this.resetPagination();
    this.loadClients(true);
  }

  // ==================== EXPORTACIÓN ====================

  getCurrentFilter(): ClientsFilter {
    return {
      page: 1,
      pageSize: this.clientsPageSize,
      sortField: this.sortField,
      sortOrder: this.sortOrder,
      registered: this.getRegisteredFilter(),
      paid: this.selectedPaid ?? undefined,
      deliveryTypes: this.selectedDeliveryTypes.length > 0 ? this.selectedDeliveryTypes : undefined,
      province: this.selectedProvince ?? undefined,
      canton: this.selectedCanton ?? undefined,
      district: this.selectedDistrict ?? undefined,
      neighborhood: this.selectedNeighborhood ?? undefined,
      daysSinceMin: this.daysRange.min ?? undefined,
      daysSinceMax: this.daysRange.max ?? undefined,
      ordersMin: this.ordersRange.min ?? undefined,
      ordersMax: this.ordersRange.max ?? undefined,
      dateFrom: this.dateRange.from ?? undefined,
      dateTo: this.dateRange.to ?? undefined,
      timeOfDay: this.selectedTimeOfDay.length > 0 ? this.selectedTimeOfDay : undefined,
      restaurant: this.selectedRestaurant.length > 0 ? this.selectedRestaurant : undefined,
      device: this.selectedDevice.length > 0 ? this.selectedDevice : undefined,
      products: this.selectedProducts.length > 0 ? this.selectedProducts : undefined,
      excludedProducts: this.selectedExcludedProducts.length > 0 ? this.selectedExcludedProducts : undefined,
      customers: this.selectedCustomers.length > 0 ? this.selectedCustomers : undefined,
    };
  }

  async exportData(): Promise<void> {
    this.exportProgressService.startExport(this.getCurrentFilter());
  }

  exportInvalidContacts(): void {
    if (this.isExportingInvalidContacts) return;
    this.isExportingInvalidContacts = true;
    const filter = this.getCurrentFilter();
    this.elasticsearchService.exportInvalidContacts(filter).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `contactos_invalidos_${new Date().toISOString().slice(0, 10)}.xlsx`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
        this.isExportingInvalidContacts = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.logger.error('Error al exportar contactos inválidos', err);
        this.isExportingInvalidContacts = false;
        this.cdr.markForCheck();
      }
    });
  }

  // ==================== CREACIÓN DE CAMPAÑAS ====================

  showCreateCampaignForm(): void {
    this.showCampaignForm = true;
  }

  closeCampaignForm(): void {
    this.showCampaignForm = false;
  }

  onCampaignSubmitted(formData: CampaignFormData): void {
    const filters = {
      registered: this.getRegisteredFilter(),
      paid: this.selectedPaid,
      deliveryType: this.selectedDeliveryTypes.length > 0 ? this.selectedDeliveryTypes : undefined,
      province: this.selectedProvince,
      canton: this.selectedCanton,
      district: this.selectedDistrict,
      neighborhood: this.selectedNeighborhood,
      daysSinceMin: this.daysRange.min,
      daysSinceMax: this.daysRange.max,
      ordersMin: this.ordersRange.min,
      ordersMax: this.ordersRange.max,
      dateFrom: this.dateRange.from,
      dateTo: this.dateRange.to,
      timeOfDay: this.selectedTimeOfDay.length > 0 ? this.selectedTimeOfDay : undefined,
      restaurant: this.selectedRestaurant.length > 0 ? this.selectedRestaurant : undefined,
      device: this.selectedDevice.length > 0 ? this.selectedDevice : undefined,
      products: this.selectedProducts.length > 0 ? this.selectedProducts : undefined,
      excludedProducts: this.selectedExcludedProducts.length > 0 ? this.selectedExcludedProducts : undefined,
      customers: this.selectedCustomers.length > 0 ? this.selectedCustomers : undefined,
    };
    const campaignRequest: CampaignRequest = { ...formData, clientFilters: filters };
    this.showCampaignForm = false;
    this.campaignProgressService.startCampaign(campaignRequest);
  }

  closePreviewModal(): void {
    this.showPreviewModal = false;
    this.previewHtml = '';
    this.previewError = '';
  }

  clearAllFilters(): void {
    // Limpiar todos los filtros
    this.selectedFilter = "all";
    this.selectedPaid = null;
    this.selectedDeliveryTypes = [];
    this.selectedProvince = null;
    this.selectedCanton = null;
    this.selectedDistrict = null;
    this.selectedNeighborhood = null;
    this.selectedTimeOfDay = [];
    this.selectedRestaurant = [];
    this.selectedDevice = [];
    this.selectedProducts = [];
    this.selectedExcludedProducts = [];
    this.selectedCustomers = [];
    this.daysRange = { min: null, max: null };
    this.ordersRange = { min: null, max: null };
    this.dateRange = { from: null, to: null };

    // Limpiar fechas del datepicker (desktop)
    this.dateRangeFrom = null;
    this.dateRangeTo = null;
    this.selectedDateRange = null;

    // Restablecer ordenamiento por defecto
    this.sortField = "idCustomer";
    this.sortOrder = "asc";
    this.sorts = [{ prop: "idCustomer", dir: "asc" }];

    // Limpiar listas dependientes
    this.cantons = [];
    this.districts = [];
    this.neighborhoods = [];

    // Recargar datos
    this.resetPagination();
    this.loadClients(true);
  }

  /**
   * Limpia todos los filtros y refresca la tabla de clientes.
   * Se ejecuta al presionar el botón de refrescar del panel de filtros.
   */
  clearAllFiltersAndRefresh(): void {
    this.logger.info("Limpiando filtros y refrescando tabla...");
    this.clearAllFilters();
  }

  onPageSizeChange(newSize: string): void {
    this.clientsPageSize = parseInt(newSize, 10) || 20;
    this.resetPagination();
    this.loadClients(true);
  }

  getPaginationPages(): number[] {
    if (!this.pagination) return [];

    const current = this.pagination.currentPage;
    const total = this.pagination.totalPages;
    const pages: number[] = [];

    // Mostrar 5 páginas: 2 antes, actual, 2 después
    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  }

  // ==================== UTILIDADES ====================
  private resetPagination(): void {
    this.currentPage = 1;
    this.hasMore = true;
  }

  private resetData(): void {
    this.clients = [];
    this.totalClients = 0;
    this.pagination = null;
  }

  getRowNumber(index: number): number {
    if (!this.pagination) return index + 1;
    return (
      (this.pagination.currentPage - 1) * this.pagination.pageSize + index + 1
    );
  }
  private formatDateToString(date: Date | string): string {
    // Si ya es string en formato YYYY-MM-DD, devolverlo directamente
    if (typeof date === "string") {
      return date;
    }
    // Si es Date, convertirlo
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
  }

  isDeliveryTypeSelected(deliveryType: string): boolean {
    return this.selectedDeliveryTypes.includes(deliveryType);
  }
  // ==================== NAVEGACIÓN ENTRE VISTAS ====================
  /**
   * Ver facturas de un cliente
   */
  viewInvoices(client: ClienteItem): void {
    if (!client.phoneCustomer) {
      this.logger.warn("No se puede ver facturas: cliente sin teléfono", {
        client: client.nameCustomer,
      });
      return;
    }
    this.selectedClient = client;
    this.currentView = "invoices";
    this.invoices = [];
    this.hasMoreInvoices = true;
    this.currentInvoicesPage = 1;
    this.loadInvoices(client.phoneCustomer, false);
  }

  /**
   * Ver direcciones de un cliente
   */
  viewAddresses(client: ClienteItem): void {
    if (!client.phoneCustomer) {
      this.logger.warn("No se puede ver direcciones: cliente sin teléfono", {
        client: client.nameCustomer,
      });
      return;
    }
    this.selectedClient = client;
    this.currentView = "addresses";
    this.addresses = [];
    this.hasMoreAddresses = true;
    this.currentAddressesPage = 1;
    this.loadAddresses(client.phoneCustomer, false);
  }

  /**
   * Volver a la lista de clientes
   */
  backToClients(): void {
    this.currentView = "clients";
    this.selectedClient = null;
    this.invoices = [];
    this.addresses = [];
  }

  /**
   * Cargar facturas de un cliente con paginación
   */
  private loadInvoices(
    phone: string,
    append: boolean = false,
    infiniteScrollEvent?: any,
  ): void {
    this.loadingInvoices = true;

    this.elasticsearchService
      .getInvoicesByPhone(
        phone,
        this.currentInvoicesPage,
        this.invoicesPageSize,
      )
      .subscribe({
        next: (response) => {
          const newInvoices = response.invoices || [];

          if (append) {
            this.invoices = [...this.invoices, ...newInvoices];
          } else {
            this.invoices = newInvoices;
          }

          this.hasMoreInvoices = response.hasMore ?? false;
          this.totalInvoicesFromServer = response.total || 0; // Guardar total del servidor

          this.loadingInvoices = false;

          // Completar el evento de infinite scroll si existe
          if (infiniteScrollEvent) {
            infiniteScrollEvent.target.complete();
          }

          this.logger.info("Facturas cargadas", {
            page: this.currentInvoicesPage,
            loaded: newInvoices.length,
            totalLoaded: this.invoices.length,
            totalServer: this.totalInvoicesFromServer,
            hasMore: this.hasMoreInvoices,
          });
        },
        error: (error) => {
          this.logger.error("Error al cargar facturas", error);
          this.loadingInvoices = false;

          // Completar el evento de infinite scroll en caso de error
          if (infiniteScrollEvent) {
            infiniteScrollEvent.target.complete();
          }

          if (!append) {
            this.invoices = [];
          }
        },
      });
  }

  /**
   * Cargar direcciones de un cliente con paginación
   */
  private loadAddresses(
    phone: string,
    append: boolean = false,
    infiniteScrollEvent?: any,
  ): void {
    this.loadingAddresses = true;

    this.elasticsearchService
      .getAddressesByPhone(
        phone,
        this.currentAddressesPage,
        this.addressesPageSize,
      )
      .subscribe({
        next: (response) => {
          const newAddresses = response.addresses || [];

          if (append) {
            this.addresses = [...this.addresses, ...newAddresses];
          } else {
            this.addresses = newAddresses;
          }

          this.hasMoreAddresses = response.hasMore ?? false;
          this.totalAddressesFromServer = response.total || 0; // Guardar total del servidor

          this.loadingAddresses = false;

          // Completar el evento de infinite scroll si existe
          if (infiniteScrollEvent) {
            infiniteScrollEvent.target.complete();
          }

          this.logger.info("Direcciones cargadas", {
            page: this.currentAddressesPage,
            loaded: newAddresses.length,
            totalLoaded: this.addresses.length,
            totalServer: this.totalAddressesFromServer,
            hasMore: this.hasMoreAddresses,
          });
        },
        error: (error) => {
          this.logger.error("Error al cargar direcciones", error);
          this.loadingAddresses = false;

          // Completar el evento de infinite scroll en caso de error
          if (infiniteScrollEvent) {
            infiniteScrollEvent.target.complete();
          }

          if (!append) {
            this.addresses = [];
          }
        },
      });
  }

  /**
   * Cargar más facturas (infinite scroll móvil)
   */
  loadMoreInvoices(event?: any): void {
    if (
      this.selectedClient?.phoneCustomer &&
      this.hasMoreInvoices &&
      !this.loadingInvoices
    ) {
      this.currentInvoicesPage++;
      this.loadInvoices(this.selectedClient.phoneCustomer, true, event);
    } else if (event) {
      // Si no se puede cargar más, completar el evento
      event.target.complete();
    }
  }

  /**
   * Cargar más direcciones (infinite scroll móvil)
   */
  loadMoreAddresses(event?: any): void {
    if (
      this.selectedClient?.phoneCustomer &&
      this.hasMoreAddresses &&
      !this.loadingAddresses
    ) {
      this.currentAddressesPage++;
      this.loadAddresses(this.selectedClient.phoneCustomer, true, event);
    } else if (event) {
      // Si no se puede cargar más, completar el evento
      event.target.complete();
    }
  }

  /**
   * Manejar cambio de página en facturas (desktop ngx-datatable)
   */
  onInvoicesPageChange(page: number): void {
    if (
      this.selectedClient?.phoneCustomer &&
      page !== this.currentInvoicesPage
    ) {
      this.currentInvoicesPage = page;
      this.loadInvoices(this.selectedClient.phoneCustomer, false);
    }
  }

  /**
   * Manejar cambio de página en direcciones (desktop ngx-datatable)
   */
  onAddressesPageChange(page: number): void {
    if (
      this.selectedClient?.phoneCustomer &&
      page !== this.currentAddressesPage
    ) {
      this.currentAddressesPage = page;
      this.loadAddresses(this.selectedClient.phoneCustomer, false);
    }
  }

  // ==================== MÉTODOS WEBSOCKET CHAT ====================

  /**
   * Conectar al WebSocket del chat
   */
  async connectWebSocket(): Promise<void> {
    await this.elasticsearchWsService.connect();
  }

  /**
   * Desconectar del WebSocket
   */
  disconnectWebSocket(): void {
    this.elasticsearchWsService.disconnect();
  }

  /**
   * Enviar mensaje al chat
   */
  sendMcpMessage(): void {
    if (!this.mcpCurrentMessage.trim()) return;
    
    this.elasticsearchWsService.sendMessage(this.mcpCurrentMessage);
    this.mcpCurrentMessage = '';
    
    // Scroll automático
    setTimeout(() => this.scrollToBottom(), 100);
  }

  /**
   * Limpiar el chat
   */
  clearMcpChat(): void {
    this.elasticsearchWsService.clearHistory();
    this.mcpCurrentMessage = '';
  }

  /**
   * Scroll automático al final del chat
   */
  private scrollToBottom(): void {
    setTimeout(() => {
      if (this.chatContainer && this.chatContainer.nativeElement) {
        this.chatContainer.nativeElement.scrollTop = 
          this.chatContainer.nativeElement.scrollHeight;
      }
    }, 100);
  }

  /**
   * Formatear texto del mensaje (permitir HTML básico)
   */
  formatMessageText(text: any): string {
    // Validar que sea string
    if (typeof text !== 'string') {
      return String(text || '');
    }
    // Convertir saltos de línea a <br>
    return text.replace(/\n/g, '<br>');
  }

  override ngOnDestroy(): void {
    // Limpiar listener del autocomplete
    document.removeEventListener("click", this.handleDocumentClick.bind(this));

    // Limpiar timeouts de búsqueda
    if (this.autocompleteTimeout) {
      clearTimeout(this.autocompleteTimeout);
    }
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }

    // Desconectar WebSocket del chat
    this.disconnectWebSocket();
  }

  // Exponer Math para el template
  Math = Math;
}
