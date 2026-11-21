import React, { useState, useEffect } from 'react';
import { Usuario, Comunidad, Publicacion, EstadoDeAnimo, Cita, Formulario, Ficha, UsuarioFicha } from './types/serena.types';
import { serenaApi } from './services/serena-api.service';
import { LoginView } from './components/LoginView';
import { Header } from './components/Header';
import { SidebarLeft } from './components/SidebarLeft';
import { HomeAprendiz } from './components/HomeAprendiz';
import { HomePsicologo } from './components/HomePsicologo';
import { EstadoDeAnimoView } from './components/EstadoDeAnimoView';
import { ComunidadView } from './components/ComunidadView';
import { CitasView } from './components/CitasView';
import { DiarioView } from './components/DiarioView';
import { FormulariosView } from './components/FormulariosView';
import { EmergenciaModal } from './components/EmergenciaModal';
import { CrearPublicacionModal } from './components/CrearPublicacionModal';
import { CrearSubComunidadModal } from './components/CrearSubComunidadModal';
import { ExpedienteClinicoModal } from './components/ExpedienteClinicoModal';
import { BackendIntegrationGuideModal } from './components/BackendIntegrationGuideModal';
import { CrearDiarioRapidoModal } from './components/CrearDiarioRapidoModal';
import { SitioEnConstruccionModal } from './components/SitioEnConstruccionModal';
import { CrearReporteModal } from './components/CrearReporteModal';

const hasStoredSession = (): boolean => {
  const hasToken = Boolean(localStorage.getItem('serena_access_token'));
  const hasUser = serenaApi.getCurrentUser().id_usuario > 0;
  if (!hasToken || !hasUser) {
    localStorage.removeItem('serena_access_token');
    localStorage.removeItem('serena_current_user');
    return false;
  }
  return true;
};

export default function App() {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(hasStoredSession);

  const [usuariosDisponibles, setUsuariosDisponibles] = useState<Usuario[]>([]);
  const [catalogoEstados, setCatalogoEstados] = useState<EstadoDeAnimo[]>([]);
  const [fichasDisponibles, setFichasDisponibles] = useState<Ficha[]>([]);
  const [relacionesUsuarioFicha, setRelacionesUsuarioFicha] = useState<UsuarioFicha[]>([]);
  const [currentUser, setCurrentUser] = useState<Usuario>(() => serenaApi.getCurrentUser());

  // Configuración de interfaz
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState<boolean>(false);
  const [idioma, setIdioma] = useState<'es' | 'en'>('es');
  const [isDarkMode, setIsDarkMode] = useState<boolean>(() => localStorage.getItem('serena_theme') === 'dark');
  const [apiError, setApiError] = useState('');

  // Navegación
  const [activeView, setActiveView] = useState<string>('home');
  const [selectedComunidadId, setSelectedComunidadId] = useState<string>('s/CMTC');

  // Estados reactivos sincronizados con serenaApi
  const [comunidades, setComunidades] = useState<Comunidad[]>([]);
  const [publicaciones, setPublicaciones] = useState<Publicacion[]>([]);
  const [historialEstados, setHistorialEstados] = useState<EstadoDeAnimo[]>([]);
  const [citas, setCitas] = useState<Cita[]>([]);
  const [formularios, setFormularios] = useState<Formulario[]>([]);
  const [votosUsuario, setVotosUsuario] = useState<Record<number, number>>({});
  const [publicacionesGuardadas, setPublicacionesGuardadas] = useState<Record<number, boolean>>({});

  // Modales
  const [isEmergenciaOpen, setIsEmergenciaOpen] = useState<boolean>(false);
  const [isCrearPubOpen, setIsCrearPubOpen] = useState<boolean>(false);
  const [isCrearSubComunidadOpen, setIsCrearSubComunidadOpen] = useState<boolean>(false);
  const [isExpedienteOpen, setIsExpedienteOpen] = useState<boolean>(false);
  const [isBackendGuideOpen, setIsBackendGuideOpen] = useState<boolean>(false);
  const [isCrearDiarioRapidoOpen, setIsCrearDiarioRapidoOpen] = useState<boolean>(false);
  const [isCrearReporteOpen, setIsCrearReporteOpen] = useState<boolean>(false);
  const [recursoModalType, setRecursoModalType] = useState<'about' | 'blog' | 'help' | null>(null);

  const [aprendizSeleccionado, setAprendizSeleccionado] = useState<Usuario | undefined>(undefined);

  useEffect(() => {
    if (!isAuthenticated) return;
    const loadUsuarios = async () => {
      try {
        const [usuarios, fichas, relaciones, estadosCatalogo, comunidadesDesdeApi] = await Promise.all([
          serenaApi.getUsuariosDesdeApi(),
          serenaApi.getFichasDesdeApi(),
          serenaApi.getUsuarioFichaDesdeApi(),
          serenaApi.getCatalogoEstadosDesdeApi(),
          serenaApi.getComunidadesDesdeApi(),
        ]);
        setUsuariosDisponibles(usuarios);
        setFichasDisponibles(fichas);
        setRelacionesUsuarioFicha(relaciones);
        setCatalogoEstados(estadosCatalogo);
        setComunidades(comunidadesDesdeApi);
        setApiError('');
      } catch (error) {
        setUsuariosDisponibles([]);
        setFichasDisponibles([]);
        setRelacionesUsuarioFicha([]);
        setCatalogoEstados([]);
        setComunidades([]);
        setApiError(error instanceof Error ? error.message : 'No se pudieron cargar los datos de la API.');
      }
    };

    void loadUsuarios();
  }, [isAuthenticated]);

  // Actualizar historial al cambiar usuario
  useEffect(() => {
    if (!isAuthenticated) return;
    const loadHistorialEstados = async () => {
      try {
        const estados = await serenaApi.getHistorialEstadosDesdeApi(currentUser.id_usuario);
        setHistorialEstados(estados);
        setApiError('');
      } catch (error) {
        setHistorialEstados([]);
        setApiError(error instanceof Error ? error.message : 'No se pudo cargar el historial de ánimo.');
      }
    };

    void loadHistorialEstados();
  }, [currentUser, isAuthenticated]);

  useEffect(() => {
    if (!isAuthenticated) return;
    let active = true;
    void Promise.all([
      serenaApi.getVotosUsuarioDesdeApi(currentUser.id_usuario),
      serenaApi.getPublicacionesGuardadasDesdeApi(currentUser.id_usuario),
    ])
      .then(([votos, guardadas]) => {
        if (!active) return;
        const voteMap: Record<number, number> = {};
        votos.upvotes.forEach((publicacion) => { voteMap[publicacion.id_publicaciones] = 1; });
        votos.downvotes.forEach((publicacion) => { voteMap[publicacion.id_publicaciones] = -1; });
        setVotosUsuario(voteMap);
        setPublicacionesGuardadas(Object.fromEntries(guardadas.map((id) => [id, true])));
      })
      .catch((error: unknown) => {
        if (active) setApiError(error instanceof Error ? error.message : 'No se pudieron cargar los votos.');
      });
    return () => { active = false; };
  }, [currentUser.id_usuario, isAuthenticated]);

  const handleLoginSuccess = (usuario: Usuario, token: string) => {
    localStorage.setItem('serena_access_token', token);
    serenaApi.setCurrentUser(usuario);
    setCurrentUser(usuario);
    setIsAuthenticated(true);
    setActiveView('home');
  };

  const handleLogout = () => {
    localStorage.removeItem('serena_access_token');
    localStorage.removeItem('serena_current_user');
    setVotosUsuario({});
    setPublicacionesGuardadas({});
    setIsAuthenticated(false);
  };

  const handleSelectComunidad = (comunidadId: string) => {
    setSelectedComunidadId(comunidadId);
    setActiveView('comunidad');
  };

  const handleVote = async (id_pub: number, delta: number) => {
    try {
      const resultado = await serenaApi.votarPublicacionEnApi(id_pub, delta);
      setVotosUsuario((previous) => {
        const next = { ...previous };
        if (resultado.voto === 'upvote') next[id_pub] = 1;
        else if (resultado.voto === 'downvote') next[id_pub] = -1;
        else delete next[id_pub];
        return next;
      });
      await handleRefreshPublicaciones();
    } catch (error) {
      setApiError(error instanceof Error ? error.message : 'No se pudo registrar el voto.');
    }
  };

  const handleToggleSave = async (id_pub: number) => {
    try {
      const resultado = await serenaApi.cambiarGuardadoPublicacionEnApi(id_pub);
      setPublicacionesGuardadas((previous) => ({
        ...previous,
        [id_pub]: resultado.guardada,
      }));
    } catch (error) {
      setApiError(error instanceof Error ? error.message : 'No se pudo guardar la publicación.');
    }
  };

  const handleRefreshPublicaciones = async () => {
    try {
      const publicacionesDesdeApi = await serenaApi.getPublicacionesDesdeApi(selectedComunidadId);
      setPublicaciones(publicacionesDesdeApi.map((publicacion) => ({
        ...publicacion,
        autor: usuariosDisponibles.find((usuario) => usuario.id_usuario === publicacion.id_usuario),
      })));
      setApiError('');
    } catch (error) {
      setPublicaciones([]);
      setApiError(error instanceof Error ? error.message : 'No se pudieron cargar las publicaciones.');
    }
  };

  const handleRefreshCitas = async () => {
    try {
      setCitas(await serenaApi.getCitasDesdeApi());
      setApiError('');
    } catch (error) {
      setCitas([]);
      setApiError(error instanceof Error ? error.message : 'No se pudieron cargar las citas.');
    }
  };

  useEffect(() => {
    void handleRefreshCitas();
    void handleRefreshPublicaciones();
  }, [selectedComunidadId, usuariosDisponibles]);

  const handleRefreshFormularios = async () => {
    try {
      setFormularios(await serenaApi.getFormulariosDesdeApi());
      setApiError('');
    } catch (error) {
      setFormularios([]);
      setApiError(error instanceof Error ? error.message : 'No se pudieron cargar los formularios.');
    }
  };

  const handleAbrirExpediente = (aprendiz: Usuario) => {
    setAprendizSeleccionado(aprendiz);
    setIsExpedienteOpen(true);
  };

  const handleAbrirDiarioAprendiz = (aprendiz: Usuario) => {
    setAprendizSeleccionado(aprendiz);
    setActiveView('diario');
  };

  const handleAgendarConPsicologo = (_psicologo: Usuario) => {
    setActiveView('citas');
  };

  const handleToggleIdioma = () => {
    setIdioma((prev) => (prev === 'es' ? 'en' : 'es'));
  };

  const handleToggleTheme = () => {
    setIsDarkMode((previous) => {
      const next = !previous;
      localStorage.setItem('serena_theme', next ? 'dark' : 'light');
      return next;
    });
  };

  const obtenerFichaUsuario = (usuario: Usuario) => {
    const relacion = relacionesUsuarioFicha.find((r) => r.id_usuario === usuario.id_usuario && r.estado !== false);
    if (relacion) {
      const fichaRelacionada = fichasDisponibles.find((f) => f.id_ficha === relacion.id_ficha);
      if (fichaRelacionada) return fichaRelacionada.codigo_ficha;
    }
    return usuario.num_ficha ?? null;
  };

  const fichaActualUsuario = obtenerFichaUsuario(currentUser);

  // Psicólogos y aprendices
  const psicologosDisponibles = usuariosDisponibles.filter((u) => {
    if (u.id_rol !== 2) return false;
    if (!fichaActualUsuario) return true;
    return obtenerFichaUsuario(u) === fichaActualUsuario;
  });
  const aprendicesDisponibles = usuariosDisponibles.filter((u) => {
    if (u.id_rol !== 1) return false;
    if (!fichaActualUsuario) return true;
    return obtenerFichaUsuario(u) === fichaActualUsuario;
  });
  const currentComunidad = comunidades.find((c) => c.id === selectedComunidadId) || comunidades[0];

  if (!isAuthenticated) {
    return <LoginView onLogin={handleLoginSuccess} />;
  }

  return (
    <div className={isDarkMode ? 'dark h-screen overflow-hidden flex flex-col font-sans antialiased selection:bg-violet-900 selection:text-violet-100' : 'h-screen overflow-hidden bg-[#FAF9FF] text-slate-900 flex flex-col font-sans antialiased selection:bg-violet-100 selection:text-violet-900'}>
      {/* Header Superior Estilo Reddit */}
      <Header
        currentUser={currentUser}
        usuariosDisponibles={usuariosDisponibles}
        onLogout={handleLogout}
        onToggleSidebar={() => setIsSidebarCollapsed(!isSidebarCollapsed)}
        isSidebarCollapsed={isSidebarCollapsed}
        onOpenCrearDiarioRapido={() => setIsCrearDiarioRapidoOpen(true)}
        onOpenCreatePublicacion={() => setIsCrearPubOpen(true)}
        onOpenBackendGuide={() => setIsBackendGuideOpen(true)}
        onOpenEmergencia={() => setIsEmergenciaOpen(true)}
        onGoHome={() => setActiveView('home')}
        isDarkMode={isDarkMode}
        onToggleTheme={handleToggleTheme}
        idioma={idioma}
        onToggleIdioma={handleToggleIdioma}
      />

      {/* Contenedor Principal: Sidebar Izquierdo Pinned Independiente + Feed Scroll */}
      <div className="flex-1 flex w-full overflow-hidden relative">
        {/* Sidebar Izquierdo con Movimiento Independiente */}
        <SidebarLeft
          currentUser={currentUser}
          activeView={activeView}
          setActiveView={setActiveView}
          comunidades={comunidades}
          selectedComunidadId={selectedComunidadId}
          onSelectComunidad={handleSelectComunidad}
          isCollapsed={isSidebarCollapsed}
          onToggleCollapse={() => setIsSidebarCollapsed(!isSidebarCollapsed)}
          onOpenCreatePublicacion={() => setIsCrearPubOpen(true)}
          onOpenCreateSubComunidad={() => setIsCrearSubComunidadOpen(true)}
          onOpenCreateFormulario={() => {
            setActiveView('formularios');
          }}
          onOpenCreateReporte={() => setIsCrearReporteOpen(true)}
          onOpenRecursoModal={(tipo) => setRecursoModalType(tipo)}
          idioma={idioma}
        />

        {/* ÁREA DE CONTENIDO CENTRAL INDEPENDIENTE */}
        <main className="flex-1 min-w-0 h-full overflow-y-auto bg-[#F8F9FA] p-3 sm:p-5 lg:p-6 relative">
          {apiError && (
            <div role="alert" className="mb-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800">
              {apiError}
            </div>
          )}

          {activeView === 'home' && (
            currentUser.id_rol === 1 ? (
              <HomeAprendiz
                currentUser={currentUser}
                publicaciones={publicaciones}
                historialEstados={historialEstados}
                psicologoAsignado={psicologosDisponibles[0]}
                ultimoEstadoAnimo={historialEstados[historialEstados.length - 1]}
                formulariosPendientes={formularios}
                onVote={handleVote}
                votosUsuario={votosUsuario}
                publicacionesGuardadas={publicacionesGuardadas}
                onToggleSave={handleToggleSave}
                onOpenEmergencia={() => setIsEmergenciaOpen(true)}
                onOpenEstadoAnimo={() => setActiveView('estado_de_animo')}
                onOpenAgendarCita={() => setActiveView('citas')}
                onOpenFormularios={() => setActiveView('formularios')}
                onSelectComunidad={handleSelectComunidad}
                idioma={idioma}
              />
            ) : (
              <HomePsicologo
                currentUser={currentUser}
                citas={citas}
                publicaciones={publicaciones}
                aprendicesSeguimiento={aprendicesDisponibles}
                onOpenCreatePublicacion={() => setIsCrearPubOpen(true)}
                onOpenCreateFormulario={() => setActiveView('formularios')}
                onRefreshCitas={handleRefreshCitas}
                onSelectAprendizDiario={handleAbrirDiarioAprendiz}
                onSelectAprendizHistoria={handleAbrirExpediente}
                onVote={handleVote}
                votosUsuario={votosUsuario}
                publicacionesGuardadas={publicacionesGuardadas}
                onToggleSave={handleToggleSave}
                idioma={idioma}
              />
            )
          )}

          {activeView === 'estado_de_animo' && (
            <EstadoDeAnimoView
              currentUser={currentUser}
              catalogoEstados={catalogoEstados}
              historialEstados={historialEstados}
              onEstadoRegistrado={async () => {
                try {
                  const estados = await serenaApi.getHistorialEstadosDesdeApi(currentUser.id_usuario);
                  setHistorialEstados(estados);
                } catch (error) {
                  setHistorialEstados([]);
                  setApiError(error instanceof Error ? error.message : 'No se pudo cargar el historial de ánimo.');
                }
              }}
            />
          )}

          {activeView === 'comunidad' && currentComunidad && (
            <ComunidadView
              comunidad={currentComunidad}
              currentUser={currentUser}
              publicaciones={publicaciones}
              usuariosDisponibles={usuariosDisponibles}
              onVote={handleVote}
              onOpenCreatePublicacion={() => setIsCrearPubOpen(true)}
              onOpenAgendarConPsicologo={handleAgendarConPsicologo}
              onOpenCreateSubComunidad={() => setIsCrearSubComunidadOpen(true)}
            />
          )}

          {(activeView === 'comunidad' || activeView === 'explorar') && !currentComunidad && (
            <div className="rounded-2xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">
              No hay comunidades registradas en la base de datos.
            </div>
          )}

          {activeView === 'explorar' && currentComunidad && (
            <ComunidadView
              comunidad={currentComunidad}
              currentUser={currentUser}
              publicaciones={publicaciones}
              usuariosDisponibles={usuariosDisponibles}
              onVote={handleVote}
              onOpenCreatePublicacion={() => setIsCrearPubOpen(true)}
              onOpenAgendarConPsicologo={handleAgendarConPsicologo}
              onOpenCreateSubComunidad={() => setIsCrearSubComunidadOpen(true)}
            />
          )}

          {activeView === 'citas' && (
            <CitasView
              currentUser={currentUser}
              citas={citas}
              psicologosDisponibles={psicologosDisponibles}
              aprendices={aprendicesDisponibles}
              onRefreshCitas={handleRefreshCitas}
            />
          )}

          {activeView === 'diario' && (
            <DiarioView
              currentUser={currentUser}
              aprendizSeleccionado={aprendizSeleccionado}
              onVolver={aprendizSeleccionado ? () => setActiveView('home') : undefined}
            />
          )}

          {activeView === 'formularios' && (
            <FormulariosView
              currentUser={currentUser}
              formularios={formularios}
              onRefreshFormularios={handleRefreshFormularios}
              onOpenCreateFormulario={() => {
                const nombre = prompt('Ingresa el título del nuevo formulario para el CMTC:');
                if (nombre) {
                  const desc = prompt('Ingresa una breve descripción:');
                  void (async () => {
                    try {
                      await serenaApi.crearFormularioEnApi({
                        nombre_formulario: nombre,
                        descripcion: desc || 'Formulario de bienestar formativo',
                        id_usuario: currentUser.id_usuario,
                        preguntas_count: 1,
                        respondido: false,
                      });
                      await handleRefreshFormularios();
                    } catch (error) {
                      console.error(error);
                    }
                  })();
                }
              }}
            />
          )}

          {activeView === 'reportes' && (
            <div className="max-w-4xl mx-auto bg-white rounded-2xl border border-slate-200 p-6 shadow-xs flex flex-col gap-4">
              <div className="flex items-center justify-between border-b border-slate-100 pb-4">
                <div>
                  <h2 className="font-black text-lg text-slate-900">
                    Reportes institucionales
                  </h2>
                  <p className="text-xs text-slate-500">
                    Centro CMTC • Ficha ADSO 3288046 • Bienestar al Aprendiz
                  </p>
                </div>
                <button
                  onClick={() => setIsCrearReporteOpen(true)}
                  className="px-4 py-2 bg-[#63C976] hover:bg-[#47A95B] text-white rounded-xl text-xs font-bold shadow-xs cursor-pointer transition-all"
                >
                  Exportar Nuevo Reporte
                </button>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-center">
                <div className="p-4 rounded-xl bg-slate-50 border border-slate-100">
                  <p className="text-2xl font-black text-[#47A95B]">{citas.length}</p>
                  <p className="text-xs font-bold text-slate-500 uppercase mt-1">Citas Registradas</p>
                </div>
                <div className="p-4 rounded-xl bg-slate-50 border border-slate-100">
                  <p className="text-2xl font-black text-[#B95FE0]">{formularios.length}</p>
                  <p className="text-xs font-bold text-slate-500 uppercase mt-1">Formularios y encuestas</p>
                </div>
                <div className="p-4 rounded-xl bg-slate-50 border border-slate-100">
                  <p className="text-2xl font-black text-slate-800">{publicaciones.length}</p>
                  <p className="text-xs font-bold text-slate-500 uppercase mt-1">Publicaciones de Apoyo</p>
                </div>
              </div>

              <div className="p-4 rounded-xl bg-emerald-50/50 border border-emerald-100 text-xs text-slate-600">
                <p className="font-bold text-[#47A95B] mb-1">Registro Inmutable de Auditoría:</p>
                <p>
                  Toda consulta o modificación realizada en la historia clínica y estados de cita queda almacenada con sello de tiempo e identificación del psicólogo actuante conforme a la Ley 1090 de 2006.
                </p>
              </div>
            </div>
          )}
        </main>
      </div>

      {/* MODALES DEL SISTEMA */}
      <EmergenciaModal
        currentUser={currentUser}
        isOpen={isEmergenciaOpen}
        onClose={() => setIsEmergenciaOpen(false)}
      />

      <CrearPublicacionModal
        currentUser={currentUser}
        comunidades={comunidades}
        defaultComunidadId={selectedComunidadId}
        isOpen={isCrearPubOpen}
        onClose={() => setIsCrearPubOpen(false)}
        onPublicacionCreada={async (publicacion) => {
          const publicacionConAutor = {
            ...publicacion,
            autor: usuariosDisponibles.find((usuario) => usuario.id_usuario === publicacion.id_usuario),
          };
          setPublicaciones((actuales) => [publicacionConAutor, ...actuales]);
          await handleRefreshPublicaciones();
        }}
      />

      <CrearSubComunidadModal
        currentUser={currentUser}
        isOpen={isCrearSubComunidadOpen}
        onClose={() => setIsCrearSubComunidadOpen(false)}
        onComunidadCreada={async (nuevaId) => {
          setComunidades(await serenaApi.getComunidadesDesdeApi());
          setSelectedComunidadId(nuevaId);
          setActiveView('comunidad');
        }}
      />

      <CrearDiarioRapidoModal
        currentUser={currentUser}
        isOpen={isCrearDiarioRapidoOpen}
        onClose={() => setIsCrearDiarioRapidoOpen(false)}
        onSaved={() => {
          if (activeView === 'diario') {
            setActiveView('home');
            setTimeout(() => setActiveView('diario'), 50);
          }
        }}
        idioma={idioma}
      />

      <CrearReporteModal
        isOpen={isCrearReporteOpen}
        onClose={() => setIsCrearReporteOpen(false)}
        currentUser={currentUser}
        idioma={idioma}
      />

      {recursoModalType && (
        <SitioEnConstruccionModal
          isOpen={!!recursoModalType}
          onClose={() => setRecursoModalType(null)}
          tipo={recursoModalType}
          idioma={idioma}
        />
      )}

      {aprendizSeleccionado && (
        <ExpedienteClinicoModal
          currentUser={currentUser}
          aprendiz={aprendizSeleccionado}
          isOpen={isExpedienteOpen}
          onClose={() => {
            setIsExpedienteOpen(false);
            setAprendizSeleccionado(undefined);
          }}
          onRefreshCitas={handleRefreshCitas}
        />
      )}

      <BackendIntegrationGuideModal
        isOpen={isBackendGuideOpen}
        onClose={() => setIsBackendGuideOpen(false)}
      />

      {/* Botón flotante persistente de emergencia 24/7 (Sólido y visible, sin parpadeos) */}
      <button
        onClick={() => setIsEmergenciaOpen(true)}
        title={idioma === 'es' ? 'Atención en Crisis y Emergencia 24/7 (Línea 106 / SENA)' : '24/7 Crisis Support (Line 106)'}
        aria-label="Botón de emergencia 24/7"
        className="fixed bottom-5 right-5 z-50 flex items-center gap-2 px-4 py-2.5 rounded-full bg-red-600 hover:bg-red-700 text-white font-bold text-xs sm:text-sm shadow-md shadow-red-900/20 border border-red-500 cursor-pointer transition-all duration-200"
      >
        <span className="w-2 h-2 rounded-full bg-white" />
        <span className="tracking-wide">SOS 24/7</span>
        <span className="hidden sm:inline text-[10px] bg-white/20 px-2 py-0.5 rounded-full font-bold">
          Línea 106
        </span>
      </button>
    </div>
  );
}
