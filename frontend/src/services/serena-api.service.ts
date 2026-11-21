import {
  Cita,
  Comunidad,
  CertificadoSoporte,
  Diario,
  Disponibilidad,
  Emergencia,
  EstadoDeAnimo,
  Formulario,
  Ficha,
  HistorialCita,
  HistorialClinico,
  Publicacion,
  Usuario,
  UsuarioFicha,
} from '../types/serena.types';

const API_BASE_URL = '/api';
const USER_SESSION_KEY = 'serena_current_user';
const ACCESS_TOKEN_KEY = 'serena_access_token';

class SerenaApiService {
  public getCurrentUser(): Usuario {
    if (typeof window === 'undefined') return { id_usuario: 0, nombre_usuario: '', email: '', id_rol: 1 };
    const saved = localStorage.getItem(USER_SESSION_KEY);
    if (!saved) return { id_usuario: 0, nombre_usuario: '', email: '', id_rol: 1 };

    try {
      const user = JSON.parse(saved) as Usuario;
      return Number.isInteger(user.id_usuario) && user.id_usuario > 0
        ? user
        : { id_usuario: 0, nombre_usuario: '', email: '', id_rol: 1 };
    } catch {
      localStorage.removeItem(USER_SESSION_KEY);
      return { id_usuario: 0, nombre_usuario: '', email: '', id_rol: 1 };
    }
  }

  public setCurrentUser(user: Usuario): void {
    localStorage.setItem(USER_SESSION_KEY, JSON.stringify(user));
  }

  public async getUsuariosDesdeApi(): Promise<Usuario[]> {
    return this.request<Usuario[]>('/Usuario');
  }

  public async getFichasDesdeApi(): Promise<Ficha[]> {
    return this.request<Ficha[]>('/Ficha');
  }

  public async getUsuarioFichaDesdeApi(): Promise<UsuarioFicha[]> {
    return this.request<UsuarioFicha[]>('/Usuario-Ficha');
  }

  public async getDisponibilidadPorUsuarioDesdeApi(id_usuario: number): Promise<Disponibilidad[]> {
    const disponibilidad = await this.request<Disponibilidad[]>('/Disponibilidad');
    return disponibilidad.filter((item) => item.id_usuario === id_usuario && item.estado !== false);
  }

  public async getComunidadesDesdeApi(): Promise<Comunidad[]> {
    return this.request<Comunidad[]>('/Comunidad');
  }

  public async crearSubComunidadEnApi(comunidad: Omit<Comunidad, 'creado_por_id'>): Promise<Comunidad> {
    return this.request<Comunidad>('/Comunidad', {
      method: 'POST',
      body: JSON.stringify(comunidad),
    });
  }

  public async getHistorialClinicoDesdeApi(id_usuario: number): Promise<HistorialClinico | null> {
    const response = await this.apiRequest(`/historial-clinico/usuario/${id_usuario}`);
    if (response.status === 404) return null;
    return this.parseResponse<HistorialClinico>(response);
  }

  public async guardarHistorialClinicoEnApi(historial: HistorialClinico): Promise<HistorialClinico> {
    const isNew = historial.id_h_clinico === 0;
    return this.request<HistorialClinico>(
      `/historial-clinico${isNew ? '/crear' : `/${historial.id_h_clinico}`}`,
      {
        method: isNew ? 'POST' : 'PUT',
        body: JSON.stringify(historial),
      }
    );
  }

  public async getHistorialCitasDesdeApi(): Promise<HistorialCita[]> {
    return this.request<HistorialCita[]>('/historial-Cita');
  }

  public async registrarObservacionCitaEnApi(
    id_cita: number,
    observaciones_historial: string
  ): Promise<HistorialCita> {
    return this.request<HistorialCita>('/historial-Cita', {
      method: 'POST',
      body: JSON.stringify({ id_cita, observaciones_historial, fecha_cambio: new Date().toISOString() }),
    });
  }

  public async getPublicacionesDesdeApi(comunidadId?: string): Promise<Publicacion[]> {
    const publicaciones = await this.request<Publicacion[]>('/Publicaciones');
    return publicaciones
      .filter((item) => !comunidadId || item.id_comunidad === comunidadId)
      .sort((a, b) => new Date(b.fecha_publicacion).getTime() - new Date(a.fecha_publicacion).getTime());
  }

  public async crearPublicacionEnApi(payload: {
    titulo: string;
    contenido: string;
    id_usuario: number;
    id_comunidad?: string;
    etiqueta?: string;
    imagen_url?: string;
    fecha_publicacion?: string;
  }): Promise<Publicacion> {
    return this.request<Publicacion>('/Publicaciones/crear', {
      method: 'POST',
      body: JSON.stringify({
        ...payload,
        id_comunidad: payload.id_comunidad || 's/CMTC',
        fecha_publicacion: payload.fecha_publicacion ?? new Date().toISOString(),
        votos: 0,
        comentarios_count: 0,
      }),
    });
  }

  public async votarPublicacionEnApi(
    idPublicacion: number,
    delta: number
  ): Promise<{ id_publicaciones: number; votos: number; voto: 'upvote' | 'downvote' | null }> {
    return this.request(`/Publicaciones/${idPublicacion}/votar`, {
      method: 'POST',
      body: JSON.stringify({ delta }),
    });
  }

  public async getVotosUsuarioDesdeApi(
    idUsuario: number
  ): Promise<{ upvotes: Publicacion[]; downvotes: Publicacion[] }> {
    return this.request(`/Publicaciones/votos/usuario/${idUsuario}`);
  }

  public async getPublicacionesGuardadasDesdeApi(idUsuario: number): Promise<number[]> {
    return this.request(`/Publicaciones/guardadas/usuario/${idUsuario}`);
  }

  public async cambiarGuardadoPublicacionEnApi(
    idPublicacion: number
  ): Promise<{ id_publicaciones: number; guardada: boolean }> {
    return this.request(`/Publicaciones/${idPublicacion}/guardar`, { method: 'POST' });
  }

  public async getSoportesClinicosDesdeApi(idUsuario: number): Promise<CertificadoSoporte[]> {
    return this.request(`/soportes-clinicos/usuario/${idUsuario}`);
  }

  public async crearSoporteClinicoEnApi(
    idUsuario: number,
    soporte: Omit<CertificadoSoporte, 'id_soporte' | 'id_usuario' | 'tipo_archivo' | 'archivo_nombre' | 'tamano_bytes'>,
    archivo: File
  ): Promise<CertificadoSoporte> {
    const formData = new FormData();
    formData.set('nombre_documento', soporte.nombre_documento);
    formData.set('entidad', soporte.entidad);
    formData.set('fecha_emision', soporte.fecha_emision);
    formData.set('medico_especialista', soporte.medico_especialista ?? '');
    formData.set('diagnostico_cie10', soporte.diagnostico_cie10 ?? '');
    formData.set('observaciones', soporte.observaciones ?? '');
    formData.set('archivo', archivo);
    return this.request(`/soportes-clinicos/usuario/${idUsuario}`, {
      method: 'POST',
      body: formData,
    });
  }

  public async descargarSoporteClinicoDesdeApi(idSoporte: number): Promise<Blob> {
    const response = await this.apiRequest(`/soportes-clinicos/${idSoporte}/archivo`);
    if (!response.ok) throw new Error(await this.getApiError(response));
    return response.blob();
  }

  public async eliminarSoporteClinicoEnApi(idSoporte: number): Promise<void> {
    await this.requestWithoutBody(`/soportes-clinicos/${idSoporte}`, { method: 'DELETE' });
  }

  public async getCitasDesdeApi(): Promise<Cita[]> {
    const citas = await this.request<Cita[]>('/Cita');
    return citas.map((cita) => ({
      ...cita,
      estado_cita: this.normalizarEstadoCita(cita.estado_cita),
    }));
  }

  public async agendarCitaEnApi(cita: Omit<Cita, 'id_cita'>): Promise<Cita> {
    return this.request<Cita>('/Cita/agendar', {
      method: 'POST',
      body: JSON.stringify(cita),
    });
  }

  public async actualizarCitaEnApi(cita: Cita): Promise<Cita> {
    return this.request<Cita>(`/Cita/${cita.id_cita}`, {
      method: 'PUT',
      body: JSON.stringify(cita),
    });
  }

  public async eliminarCitaEnApi(id_cita: number): Promise<void> {
    await this.requestWithoutBody(`/Cita/${id_cita}`, { method: 'DELETE' });
  }

  public async cambiarEstadoCitaEnApi(cita: Cita, estado_cita: Cita['estado_cita']): Promise<Cita> {
    return this.actualizarCitaEnApi({ ...cita, estado_cita });
  }

  public async getDiarioDesdeApi(id_usuario: number): Promise<Diario | null> {
    const response = await this.apiRequest(`/Diario/Usuario/${id_usuario}`);
    if (response.status === 404) return null;
    return this.parseResponse<Diario>(response);
  }

  public async actualizarDiarioEnApi(entrada: {
    id_usuario: number;
    contenido: string;
    compartir_sp: boolean;
  }): Promise<Diario> {
    return this.request<Diario>('/Diario/crear', {
      method: 'POST',
      body: JSON.stringify({ ...entrada, fecha_apertura: new Date().toISOString() }),
    });
  }

  public async cambiarPermisoCompartirDiarioEnApi(diario: Diario, compartir_sp: boolean): Promise<Diario> {
    return this.request<Diario>(`/Diario/${diario.id_diario}`, {
      method: 'PUT',
      body: JSON.stringify({ ...diario, compartir_sp }),
    });
  }

  public async getCatalogoEstadosDesdeApi(): Promise<EstadoDeAnimo[]> {
    return this.request<EstadoDeAnimo[]>('/estado-animo');
  }

  public async getHistorialEstadosDesdeApi(id_usuario: number): Promise<EstadoDeAnimo[]> {
    return this.request<EstadoDeAnimo[]>(`/estado-animo-Usuario/Usuario/${id_usuario}`);
  }

  public async registrarEstadoDeAnimoEnApi(payload: {
    id_usuario: number;
    id_estado: number;
    fecha_estado?: string;
    motivo?: string;
  }): Promise<EstadoDeAnimo> {
    return this.request<EstadoDeAnimo>('/estado-animo-Usuario', {
      method: 'POST',
      body: JSON.stringify({
        ...payload,
        fecha_estado: payload.fecha_estado ?? new Date().toISOString(),
        motivo: payload.motivo ?? '',
      }),
    });
  }

  public async registrarEmergencia(id_usuario: number, descripcion: string): Promise<Emergencia> {
    return this.request<Emergencia>('/Emergencia', {
      method: 'POST',
      body: JSON.stringify({ id_usuario, descripcion, fecha_emergencia: new Date().toISOString() }),
    });
  }

  public async actualizarEmergenciaEnApi(emergencia: Emergencia): Promise<Emergencia> {
    return this.request<Emergencia>(`/Emergencia/${emergencia.id_emergencia}`, {
      method: 'PUT',
      body: JSON.stringify(emergencia),
    });
  }

  public async getEmergenciasDesdeApi(): Promise<Emergencia[]> {
    return this.request<Emergencia[]>('/Emergencia');
  }

  public async getFormulariosDesdeApi(): Promise<Formulario[]> {
    return this.request<Formulario[]>('/Formulario');
  }

  public async crearFormularioEnApi(payload: {
    nombre_formulario: string;
    descripcion: string;
    id_usuario: number;
    preguntas_count?: number;
    tiempo_estimado?: string;
    id_comunidad?: string;
    respondido?: boolean;
  }): Promise<Formulario> {
    return this.request<Formulario>('/Formulario', {
      method: 'POST',
      body: JSON.stringify({
        ...payload,
        preguntas_count: payload.preguntas_count ?? 1,
        tiempo_estimado: payload.tiempo_estimado ?? '3 minutos',
        respondido: payload.respondido ?? false,
      }),
    });
  }

  public async responderFormularioEnApi(id_formulario: number, respuesta: string): Promise<void> {
    await this.requestWithoutBody(`/Formulario/${id_formulario}/responder`, {
      method: 'POST',
      body: JSON.stringify({ respuesta }),
    });
  }

  private normalizarEstadoCita(estado?: string): Cita['estado_cita'] {
    const capitalizado = estado
      ? estado.charAt(0).toUpperCase() + estado.slice(1).toLowerCase()
      : 'Pendiente';
    switch (capitalizado) {
      case 'Confirmada':
      case 'Realizada':
      case 'Cancelada':
      case 'Pendiente':
        return capitalizado;
      default:
        return 'Pendiente';
    }
  }

  private async request<T>(path: string, init: RequestInit = {}): Promise<T> {
    return this.parseResponse<T>(await this.apiRequest(path, init));
  }

  private async requestWithoutBody(path: string, init: RequestInit): Promise<void> {
    const response = await this.apiRequest(path, init);
    if (!response.ok) throw new Error(await this.getApiError(response));
  }

  private async parseResponse<T>(response: Response): Promise<T> {
    if (!response.ok) throw new Error(await this.getApiError(response));
    if (response.status === 204) throw new Error('La API no devolvió el contenido esperado.');
    return response.json() as Promise<T>;
  }

  private apiRequest(path: string, init: RequestInit = {}): Promise<Response> {
    const headers = new Headers(init.headers);
    if (init.body !== undefined && !(init.body instanceof FormData)) {
      headers.set('Content-Type', 'application/json');
    }
    const token = localStorage.getItem(ACCESS_TOKEN_KEY);
    if (token) headers.set('Authorization', `Bearer ${token}`);
    return fetch(`${API_BASE_URL}${path}`, { ...init, headers });
  }

  private async getApiError(response: Response): Promise<string> {
    const body = await response.text();
    if (!body) return `La API respondió ${response.status}.`;
    try {
      const json = JSON.parse(body);
      return json.mensaje || json.title || body;
    } catch {
      return body;
    }
  }
}

export const serenaApi = new SerenaApiService();
